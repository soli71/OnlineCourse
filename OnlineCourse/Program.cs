using Azure;
using Microsoft.AspNetCore.Authentication.BearerToken;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using Minio;
using OnlineCourse;
using OnlineCourse.Contexts;
using OnlineCourse.Controllers.Site;
using OnlineCourse.Identity;
using OnlineCourse.Identity.Entities;
using OnlineCourse.Orders.Services;
using OnlineCourse.Products.Services;
using OnlineCourse.Services;
using System.Net.Http;
using System.Text.Json;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = 1000 * 1024 * 1024;
});

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi

// 1. فعال‌سازی خواندن هدرهای فوروارد
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    // هدرهای مدنظر
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;

    // اگر می‌خواهید همهٔ پروکسی‌ها رو قبول کنید (اختیاری)
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();

    // در صورت تمایل می‌توانید IP سرور Next.js رو به عنوان پروکسی معتبر اضافه کنید:
    // options.KnownProxies.Add(IPAddress.Parse("10.0.0.5"));
});

//builder.Services.AddOpenApi();
builder.Services.AddScoped<ISmsService, SmsService>();
builder.Services.AddScoped<PhysicalProductService>();
builder.Services.AddScoped<LicenseService>();
builder.Services.AddSwaggerGen(c =>
{
    // 1. دو سند مجزا: site و panel
    c.SwaggerDoc("site", new OpenApiInfo { Title = "Site APIs", Version = "v1" });
    c.SwaggerDoc("panel", new OpenApiInfo { Title = "Panel APIs", Version = "v1" });

    // 2. فیلتر بر اساس بخشِ route
    c.DocInclusionPredicate((docName, apiDesc) =>
    {
        var path = apiDesc.RelativePath?.ToLower() ?? "";

        return docName switch
        {
            "site" => path.Contains("site"),    // فقط endpoints با api/site/…
            "panel" => path.Contains("panel"),       // یا panel/…
            _ => false
        };
    });

    // ۳. بقیهٔ تنظیماتِ عمومی (security, schema ids, …)
    c.CustomSchemaIds(type => type.FullName);
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Bearer authorization",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement {
        {
            new OpenApiSecurityScheme {
                Reference = new OpenApiReference {
                    Type = ReferenceType.SecurityScheme,
                    Id   = "Bearer"
                },
                Scheme = "oauth2",
                Name   = "Bearer",
                In     = ParameterLocation.Header
            },
            new List<string>()
        }
    });
});

builder.Services.AddScoped<ISaveChangesInterceptor, AuditInterceptor>();

builder.Services.AddDbContext<ApplicationDbContext>((sp, options) =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"))
    .AddInterceptors(sp.GetServices<ISaveChangesInterceptor>())
    );

builder.Services.AddIdentityApiEndpoints<User>(c =>
{
    c.SignIn.RequireConfirmedPhoneNumber = true;
})
    .AddRoles<Role>()
     .AddErrorDescriber<CustomIdentityErrorDescriber>()
    .AddEntityFrameworkStores<ApplicationDbContext>();

builder.Services.AddMemoryCache();

var domain = Environment.GetEnvironmentVariable("DOMAIN");

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReactApp", builder =>
    {
        builder.WithOrigins(domain.Split(',')) // React app's URL
               .AllowAnyHeader()
               .AllowAnyMethod()
               .AllowCredentials();
    });
});

builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<IConfigureOptions<BearerTokenOptions>, CustomBearerTokenOption>());

builder.Services.AddMinio(c =>
{
    c.WithEndpoint(builder.Configuration["MinIO:Endpoint"]);
    c.WithCredentials(builder.Configuration["MinIO:AccessKey"], builder.Configuration["MinIO:SecretKey"]);
});
builder.Services.AddScoped<IMinioService, MinioService>();
builder.Services.AddScoped<ICourseCapacityService, CourseCapacityService>();
builder.Services.AddScoped<ISpotPlayerService, SpotPlayerService>();
builder.Services.AddEndpointsApiExplorer();

var globalLimiterOptions = builder.Configuration.GetSection("RateLimit:GlobalLimiterOptions")?.Get<GlobalLimiterOptions>() ?? new();
var enableRateLimit = bool.Parse(builder.Configuration["RateLimit:Enabled"] ?? "false");
if (enableRateLimit)
{
    builder.Services.AddRateLimiter(options =>
    {
        if (globalLimiterOptions.Enabled)
        {
            var limiters = new List<PartitionedRateLimiter<HttpContext>>();

            if (globalLimiterOptions.GlobalFixedWindowLimiterOptions.Enabled)
            {
                var fixedWindowLimiter = globalLimiterOptions.GlobalFixedWindowLimiterOptions.FixedWindowRateLimiterOptions;
                var fixedLimit = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
                {
                    var key = httpContext.User.Identity?.IsAuthenticated == true
                    ? httpContext.User.Identity.Name!
                    : httpContext.Connection.RemoteIpAddress!.ToString()!;
                    return RateLimitPartition.GetFixedWindowLimiter(key, _ => fixedWindowLimiter);
                });
                limiters.Add(fixedLimit);
            }

            if (globalLimiterOptions.GlobalTokenBucketLimiterOptions.Enabled)
            {
                var tokenBucketLimiter = globalLimiterOptions.GlobalTokenBucketLimiterOptions.TokenBucketRateLimiterOptions;
                var bucketLimit = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
                {
                    var key = httpContext.User.Identity?.IsAuthenticated == true
                ? httpContext.User.Identity.Name!
                : httpContext.Connection.RemoteIpAddress!.ToString()!;
                    return RateLimitPartition.GetTokenBucketLimiter(key, _ => tokenBucketLimiter);
                });
                limiters.Add(bucketLimit);
            }

            if (globalLimiterOptions.GlobalConcurrencyLimiterOptions.Enabled)
            {
                var concurrencyLimiter = globalLimiterOptions.GlobalConcurrencyLimiterOptions.ConcurrencyLimiterOptions;
                var concurrencyLimit = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
                {
                    var key = httpContext.User.Identity?.IsAuthenticated == true
             ? httpContext.User.Identity.Name!
             : httpContext.Connection.RemoteIpAddress!.ToString()!;
                    return RateLimitPartition.GetConcurrencyLimiter(key, _ => concurrencyLimiter);
                }
                );
                limiters.Add(concurrencyLimit);
            }

            options.GlobalLimiter = PartitionedRateLimiter.CreateChained(limiters.ToArray());
        }
        else
            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
            {
                var key = httpContext.User.Identity?.IsAuthenticated == true
? httpContext.User.Identity.Name!
: httpContext.Connection.RemoteIpAddress!.ToString()!;

                return RateLimitPartition.GetTokenBucketLimiter("DefaultRateLimiter",
                 _ => new TokenBucketRateLimiterOptions
                 {
                     TokenLimit = 1000,
                     AutoReplenishment = true,
                     ReplenishmentPeriod = TimeSpan.FromSeconds(1),
                     TokensPerPeriod = 10,
                     QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                     QueueLimit = 2,
                 });
            });

        options.OnRejected = async (context, cancellationToken) =>
        {
            context.HttpContext.Response.StatusCode = 429; // Too Many Requests
            context.HttpContext.Response.ContentType = "application/json";
            var error = new ApiResult(false, "تعداد درخواست‌های شما بیش از حد مجاز است. لطفاً بعداً تلاش کنید.", null, 429);
            await context.HttpContext.Response.WriteAsync(JsonSerializer.Serialize(error), cancellationToken);
        };
    });
}

builder.Services.AddOutputCache();
builder.Services.AddHttpClient();
var app = builder.Build();

app.UseForwardedHeaders();

app.Use(async (context, next) =>
{
    try
    {
        try
        {
            if (bool.TryParse(Environment.GetEnvironmentVariable("EnableVisitLog"), out bool enableVisitLog) && enableVisitLog)
            {
                string loadDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Visit");
                Directory.CreateDirectory(loadDirectory);
                string logFile = Path.Combine(loadDirectory, $"Visit_{DateTime.Now:yyyyMMdd}.log");
                string requestPath = context.Request.Path.ToString();
                string requestMethod = context.Request.Method.ToString();
                var ip = context.Connection.RemoteIpAddress?.ToString() ?? "Unknown IP";
                var userAgent = context.Request.Headers["User-Agent"].ToString() ?? "Unknown User Agent";
                var referer = context.Request.Headers["Referer"].ToString() ?? "Unknown Referer";

                var message = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] Request  : {requestMethod} {requestPath} \r\n" +
                              $"User Agent: {userAgent}\r\n" +
                              $"Referer: {referer}\r\n" +
                              $"IP : {ip}\r\n";

                File.AppendAllTextAsync(logFile, message);
            }
        }
        catch { }
        ;
        await next(context);

        if (context.Response.StatusCode == 403)
        {
            var apiResult = new ApiResult(false, "شما مجاز به دسترسی به این بخش نیستید", null, 403);
            context.Response.ContentType = "application/json";
            context.Response.Headers.Add("Access-Control-Allow-Origin", "*");
            await context.Response.WriteAsync(JsonSerializer.Serialize(apiResult));
        }
    }
    catch (Exception ex)
    {
        // Write to file
        string loadDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs");
        Directory.CreateDirectory(loadDirectory);

        string logFile = Path.Combine(loadDirectory, $"Error_{DateTime.Now:yyyyMMdd}.log");
        string errorMessage = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] Exception  : {ex.Message} \r\n" +
        $"Stack Trace: {ex.StackTrace}\r\n" +
        $"____________________________________________________________________\r\n";
        await File.AppendAllTextAsync(logFile, errorMessage);
        context.Response.StatusCode = 500;
        context.Response.ContentType = "application/json";
        var error = new ApiResult(false, "خطا در سرور", null, 500);
        await context.Response.WriteAsync(JsonSerializer.Serialize(error));
    }
});

//using (var scope = app.Services.CreateScope())
//{
//    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
//    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<Role>>();
//    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
//    var adminRole = new Role { Name = "Admin" };
//    var userRole = new Role { Name = "User" };
//    var panelRole = new Role { Name = "Panel" };
//    if (!context.Roles.Any())
//    {
//        roleManager.CreateAsync(adminRole).Wait();
//        roleManager.CreateAsync(userRole).Wait();
//        roleManager.CreateAsync(panelRole).Wait();
//    }
//    if (!context.Users.Any(c => c.Type == UserType.Admin))
//    {
//        var adminUser = new User
//        {
//            UserName = "Admin@Panel.com",
//            PhoneNumber = "09338181361",
//            Type = UserType.Admin,
//            PhoneNumberConfirmed = true,
//            EmailConfirmed = true
//        };
//        userManager.CreateAsync(adminUser, "Admin@123").Wait();
//        userManager.AddToRoleAsync(adminUser, "Admin").Wait();
//    }
//}

app.UseOutputCache();
//if (app.Environment.IsDevelopment())
//{
//app.MapOpenApi();
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    // آدرس JSON برای هر سند
    c.SwaggerEndpoint("/swagger/site/swagger.json", "Site APIs V1");
    c.SwaggerEndpoint("/swagger/panel/swagger.json", "Panel APIs V1");

    // اگر بخواهید یکی را به‌صورت پیش‌فرض باز کنید:
    c.RoutePrefix = string.Empty; // swagger در ریشه‌ی / قرار می‌گیرد
    c.DefaultModelsExpandDepth(-1);
});
//app.MapScalarApiReference();
//}

if (enableRateLimit)
    app.UseRateLimiter(new Microsoft.AspNetCore.RateLimiting.RateLimiterOptions
    {
        GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
            RateLimitPartition.GetTokenBucketLimiter("DefaultRateLimiter",
             _ => new TokenBucketRateLimiterOptions
             {
                 TokenLimit = 1000,
                 AutoReplenishment = true,
                 ReplenishmentPeriod = TimeSpan.FromSeconds(1),
                 TokensPerPeriod = 10,
                 QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                 QueueLimit = 2,
             }))
    });

app.UseStaticFiles();
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.UseCors("AllowReactApp");

app.MapControllers();
app.MapMyIdentityApi<User>();

app.Run();