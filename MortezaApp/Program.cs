using Microsoft.AspNetCore.Authentication.BearerToken;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;
using OnlineCourse.Entities;
using OnlineCourse.Contexts;
using OnlineCourse.Services;
using OnlineCourse;
using OnlineCourse.Identity;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

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
var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI();
    //app.MapScalarApiReference();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.UseCors("AllowReactApp");

app.MapControllers();
app.MapMyIdentityApi<User>();

app.Run();