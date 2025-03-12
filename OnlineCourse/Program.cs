using Microsoft.AspNetCore.Authentication.BearerToken;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Minio;
using OnlineCourse;
using OnlineCourse.Contexts;
using OnlineCourse.Controllers.Site;
using OnlineCourse.Entities;
using OnlineCourse.Identity;
using OnlineCourse.RateLimiters;
using OnlineCourse.Services;
using System.Diagnostics;
using System.IdentityModel.Tokens.Jwt;
using System.Text.Json;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = 300 * 1024 * 1024;
});

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi

builder.Services.AddScoped<ISmsService, SmsService>();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "My API",
        Version = "v1",
        Description = "API documentation for My API",
        TermsOfService = new Uri("https://example.com/terms"),
        Contact = new Microsoft.OpenApi.Models.OpenApiContact
        {
            Name = "Support",
            Email = "support@example.com",
            Url = new Uri("https://example.com/support")
        },
        License = new Microsoft.OpenApi.Models.OpenApiLicense
        {
            Name = "Use under LICX",
            Url = new Uri("https://example.com/license")
        }
    });

    c.CustomSchemaIds(type => type.FullName);

    c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Example: 'Bearer {token}'",
        Name = "Authorization",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement()
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                },
                Scheme = "oauth2",
                Name = "Bearer",
                In = Microsoft.OpenApi.Models.ParameterLocation.Header
            },
            new List<string>()
        }
    });
});

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

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
    c.WithEndpoint("minio.chbk.app");
    c.WithCredentials("qluHNnovlTLh9zs4R0BR4G4UlroPMTef", "a6jvqg6wQwbendIMFZcEum35lTZrbIiO");
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
                var fixedLimit = PartitionedRateLimiter.Create<HttpContext, string>(context =>
                    RateLimitPartition.GetFixedWindowLimiter("FixedWindowLimiter", _ => fixedWindowLimiter));
                limiters.Add(fixedLimit);
            }

            if (globalLimiterOptions.GlobalTokenBucketLimiterOptions.Enabled)
            {
                var tokenBucketLimiter = globalLimiterOptions.GlobalTokenBucketLimiterOptions.TokenBucketRateLimiterOptions;
                var bucketLimit = PartitionedRateLimiter.Create<HttpContext, string>(context =>
                    RateLimitPartition.GetTokenBucketLimiter("TokenBucketLimiter", _ => tokenBucketLimiter));
                limiters.Add(bucketLimit);
            }

            if (globalLimiterOptions.GlobalConcurrencyLimiterOptions.Enabled)
            {
                var concurrencyLimiter = globalLimiterOptions.GlobalConcurrencyLimiterOptions.ConcurrencyLimiterOptions;
                var concurrencyLimit = PartitionedRateLimiter.Create<HttpContext, string>(context =>
                    RateLimitPartition.GetConcurrencyLimiter("ConcurrencyLimiter", _ => concurrencyLimiter));
                limiters.Add(concurrencyLimit);
            }

            options.GlobalLimiter = PartitionedRateLimiter.CreateChained(limiters.ToArray());
        }
        else
            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
            RateLimitPartition.GetTokenBucketLimiter("DefaultRateLimiter",
             _ => new TokenBucketRateLimiterOptions
             {
                 TokenLimit = 1000,
                 AutoReplenishment = true,
                 ReplenishmentPeriod = TimeSpan.FromSeconds(1),
                 TokensPerPeriod = 10,
                 QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                 QueueLimit = 2,
             }));

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

using (var scope = app.Services.CreateScope())
{
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<Role>>();
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var adminRole = new Role { Name = "Admin" };
    var userRole = new Role { Name = "User" };
    var panelRole = new Role { Name = "Panel" };
    if (!context.Roles.Any())
    {
        roleManager.CreateAsync(adminRole).Wait();
        roleManager.CreateAsync(userRole).Wait();
        roleManager.CreateAsync(panelRole).Wait();
    }
    if (!context.Users.Any(c => c.Type == UserType.Admin))
    {
        var adminUser = new User
        {
            UserName = "Admin@Panel.com",
            PhoneNumber = "09338181361",
            Type = UserType.Admin,
            PhoneNumberConfirmed = true,
            EmailConfirmed = true
        };
        userManager.CreateAsync(adminUser, "Admin@123").Wait();
        userManager.AddToRoleAsync(adminUser, "Admin").Wait();
    }
}

app.UseOutputCache();
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    //app.MapScalarApiReference();
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
;
if (enableRateLimit)
    app.UseRateLimiter();

app.UseStaticFiles();
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.UseCors("AllowReactApp");

app.MapControllers();
app.MapMyIdentityApi<User>();

app.Run();