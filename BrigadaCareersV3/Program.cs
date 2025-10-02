using BrigadaCareersV3Library.Auth;
using BrigadaCareersV3Library.AuthServices;
using BrigadaCareersV3Library.Entities;
using JobPostingLibrary.Entities;
using JobPostingLibrary.HrmsServices;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Security.Claims;
using System.Text;
// ?? add this
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// Controllers + System.Text.Json enum serialization as strings
builder.Services
    .AddControllers()
    .AddJsonOptions(o =>
    {
        // Makes enums serialize as their names (e.g., "Pending"), not integers.
        o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

builder.Services.AddControllersWithViews();
builder.Services.AddSession();

// Swagger (Swashbuckle)
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// DbContexts
builder.Services.AddDbContext<ApplicationDbContext>(opt =>
    opt.UseSqlServer(builder.Configuration.GetConnectionString("DefaultCon")));
builder.Services.AddDbContext<BrigadaCareersDbv3Context>(opt =>
    opt.UseSqlServer(builder.Configuration.GetConnectionString("DefaultCon")));
builder.Services.AddDbContext<PreProdHrmsParallelContext>(opt =>
    opt.UseSqlServer(builder.Configuration.GetConnectionString("HrmsConnection")));

builder.Services.AddScoped<IUserAuthenticationService, UserAuthenticationService>();
builder.Services.AddScoped<IHrmsService, HrmsService>();
builder.Services.AddMemoryCache();

// Identity
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

// JWT
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
            Encoding.UTF8.GetBytes(builder.Configuration["JWT:SecreteKey"]!)),
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

// CORS
builder.Services.AddCors(o => o.AddPolicy("AllowSpa", p =>
    p.WithOrigins("http://localhost:4200").AllowAnyHeader().AllowAnyMethod().AllowCredentials()
));

// NSwag document (optional, you already consume it via nswag.json/useDocumentProvider)
builder.Services.AddOpenApiDocument(config =>
{
    config.Title = "BrigadaCareers API";
    config.Description = "API documentation for BrigadaCareers using NSwag.";
    config.Version = "v3";

    // NSwag uses NJsonSchema with System.Text.Json; it will honor the JsonStringEnumConverter above.
    // If you ever need to force it at the document layer, you can also do:
    // if (config.SchemaGenerator.Settings is NJsonSchema.Generation.SystemTextJsonSchemaGeneratorSettings stj)
    // {
    //     stj.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
    // }
});

builder.Services.AddHttpClient("nominatim", client =>
{
    client.BaseAddress = new Uri("https://nominatim.openstreetmap.org/");
    client.DefaultRequestHeaders.UserAgent
          .ParseAdd("BrigadaCareers/1.0 (mercadoblaise@gmail.com)");
});

// Optional extra CORS policy
builder.Services.AddCors(o => o.AddPolicy("front", p => p
    .WithOrigins("https://localhost:44381", "https://localhost:44381")
    .AllowAnyHeader().AllowAnyMethod().AllowCredentials()
));

var app = builder.Build();

// Pipeline
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
app.UseDeveloperExceptionPage();
app.UseHttpsRedirection();

app.UseRouting();
app.UseCors("AllowSpa");
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
