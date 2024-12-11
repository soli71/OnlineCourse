using Microsoft.AspNetCore.Authentication.BearerToken;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Minio;
using OnlineCourse;
using OnlineCourse.Contexts;
using OnlineCourse.Entities;
using OnlineCourse.Identity;
using OnlineCourse.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = 300 * 1024 * 1024;
});

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
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
    c.WithEndpoint("minio-nnw4iz.chbk.app");
    c.WithCredentials("YDfsZiXN2gn0WuejtK4mgCJ1dbcjKMU4", "o4x19QwFUMUk6Lcmt4LVvCgVIcgdIEUe");
});
builder.Services.AddScoped<IMinioService, MinioService>();
builder.Services.AddEndpointsApiExplorer();
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
            PhoneNumberConfirmed=true,
            EmailConfirmed=true
        };
        userManager.CreateAsync(adminUser, "Admin@123").Wait();
        userManager.AddToRoleAsync(adminUser, "Admin").Wait();
    }
}
// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI();
    //app.MapScalarApiReference();
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
};

app.UseStaticFiles();
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.UseCors("AllowReactApp");

app.MapControllers();
app.MapMyIdentityApi<User>();


app.Run();