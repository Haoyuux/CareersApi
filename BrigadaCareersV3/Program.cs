using Amazon.S3;
using Amazon;
using BrigadaCareersV3Library.Amazon;
using BrigadaCareersV3Library.Auth;
using BrigadaCareersV3Library.AuthServices;
using BrigadaCareersV3Library.Entities;
using JobPostingLibrary.Entities;
using JobPostingLibrary.HrmsServices;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Security.Claims;
using System.Text;
using System.Text.Json.Serialization;
using BrigadaCareersV3Library.OtpServices;
using BrigadaCareersV3Library.Dto;

var builder = WebApplication.CreateBuilder(args);

// ---------------------------------------
// Controllers and JSON enum serialization
// ---------------------------------------
builder.Services
    .AddControllers()
    .AddJsonOptions(o =>
    {
        o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

builder.Services.AddControllersWithViews();
builder.Services.AddSession();

// ---------------------------------------
// Swagger / API explorer
// ---------------------------------------
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// ---------------------------------------
// Database contexts
// ---------------------------------------
builder.Services.AddDbContext<ApplicationDbContext>(opt =>
    opt.UseSqlServer(builder.Configuration.GetConnectionString("DefaultCon")));

builder.Services.AddDbContext<BrigadaCareersDbv3Context>(opt =>
    opt.UseSqlServer(builder.Configuration.GetConnectionString("DefaultCon")));

builder.Services.AddDbContext<PreProdHrmsParallelContext>(opt =>
    opt.UseSqlServer(builder.Configuration.GetConnectionString("HrmsConnection")));

// ---------------------------------------
// Dependency injection for application services
// ---------------------------------------
builder.Services.AddScoped<IUserAuthenticationService, UserAuthenticationService>();
builder.Services.AddScoped<IHrmsService, HrmsService>();
builder.Services.AddScoped<OtpService>();

builder.Services.Configure<SmtpSettings>(builder.Configuration.GetSection("SmtpSettings"));
builder.Services.AddMemoryCache();

// ---------------------------------------
// AWS + S3 configuration
// ---------------------------------------

// Load AWS settings from appsettings.json
builder.Services.Configure<AwsSettings>(
    builder.Configuration.GetSection("AwsSettings")
);

// Register AWS S3 client
builder.Services.AddSingleton<IAmazonS3>(sp =>
{
    var awsSettings = sp.GetRequiredService<IOptions<AwsSettings>>().Value;
    var config = new AmazonS3Config
    {
        RegionEndpoint = RegionEndpoint.GetBySystemName(awsSettings.Region)
    };
    return new AmazonS3Client(awsSettings.AccessKey, awsSettings.SecretKey, config);
});

// Register S3 service for DI
builder.Services.AddScoped<S3AmazonServices>();

// ---------------------------------------
// ASP.NET Identity configuration
// ---------------------------------------
builder.Services.AddIdentity<ApplicationIdentityUser, IdentityRole>(options =>
{
    options.Password.RequiredLength = 6;
    options.Password.RequireLowercase = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireDigit = false;
    options.Tokens.PasswordResetTokenProvider = TokenOptions.DefaultProvider;
    options.SignIn.RequireConfirmedAccount = true;
})
.AddRoles<IdentityRole>()
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders()
.AddTokenProvider("userIdentity", typeof(DataProtectorTokenProvider<ApplicationIdentityUser>));

// ---------------------------------------
// JWT configuration
// ---------------------------------------
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.SaveToken = false;
    options.RequireHttpsMetadata = false;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ClockSkew = TimeSpan.Zero,
        ValidAudience = builder.Configuration["JWT:ValidAudience"],
        ValidIssuer = builder.Configuration["JWT:ValidIssuer"],
        IssuerSigningKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(builder.Configuration["JWT:SecreteKey"]!)
        ),
        NameClaimType = ClaimTypes.NameIdentifier,
        RoleClaimType = ClaimTypes.Role
    };

    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = ctx =>
        {
            Console.WriteLine("Auth header: " + ctx.Request.Headers["Authorization"].ToString());
            return Task.CompletedTask;
        },
        OnAuthenticationFailed = ctx =>
        {
            Console.WriteLine("JWT auth failed: " + ctx.Exception.Message);
            return Task.CompletedTask;
        },
        OnTokenValidated = ctx =>
        {
            Console.WriteLine("JWT validated for principal: " + (ctx.Principal?.Identity?.Name ?? "(no name)"));
            return Task.CompletedTask;
        }
    };
});

// ---------------------------------------
// CORS
// ---------------------------------------
builder.Services.AddCors(o => o.AddPolicy("AllowSpa", p =>
    p.WithOrigins("http://localhost:4200")
     .AllowAnyHeader()
     .AllowAnyMethod()
     .AllowCredentials()
));

// Optional extra policy
builder.Services.AddCors(o => o.AddPolicy("front", p =>
    p.WithOrigins("https://localhost:44381", "https://localhost:44381")
     .AllowAnyHeader()
     .AllowAnyMethod()
     .AllowCredentials()
));

// ---------------------------------------
// NSwag / OpenAPI
// ---------------------------------------
builder.Services.AddOpenApiDocument(config =>
{
    config.Title = "BrigadaCareers API";
    config.Description = "API documentation for BrigadaCareers using NSwag.";
    config.Version = "v3";
});

// ---------------------------------------
// HTTP clients
// ---------------------------------------
builder.Services.AddHttpClient("nominatim", client =>
{
    client.BaseAddress = new Uri("https://nominatim.openstreetmap.org/");
    client.DefaultRequestHeaders.UserAgent.ParseAdd("BrigadaCareers/1.0 (mercadoblaise@gmail.com)");
});

// ---------------------------------------
// Build application
// ---------------------------------------
var app = builder.Build();

// ---------------------------------------
// Middleware pipeline
// ---------------------------------------
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.UseDeveloperExceptionPage();
    app.UseForwardedHeaders();
}
else
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHsts();
app.UseHttpsRedirection();

app.UseRouting();
app.UseCors("AllowSpa");
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
