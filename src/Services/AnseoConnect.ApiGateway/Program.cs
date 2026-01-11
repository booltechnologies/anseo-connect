using AnseoConnect.Data;
using AnseoConnect.Data.Entities;
using AnseoConnect.Data.MultiTenancy;
using AnseoConnect.ApiGateway.Middleware;
using AnseoConnect.ApiGateway.Services;
using AnseoConnect.Shared;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddOpenApi();

// Database configuration
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") 
    ?? Environment.GetEnvironmentVariable("ANSEO_SQL")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' or environment variable 'ANSEO_SQL' not found.");

builder.Services.AddDbContext<AnseoConnectDbContext>(options =>
    options.UseSqlServer(connectionString));

// Service Bus configuration (for webhooks)
var serviceBusConnectionString = builder.Configuration.GetConnectionString("ServiceBus")
    ?? Environment.GetEnvironmentVariable("ANSEO_SERVICEBUS");

if (!string.IsNullOrEmpty(serviceBusConnectionString))
{
    builder.Services.AddSingleton<IMessageBus>(sp =>
    {
        var logger = sp.GetRequiredService<ILogger<ServiceBusMessageBus>>();
        return new ServiceBusMessageBus(serviceBusConnectionString, logger);
    });
}

// TenantContext - scoped per request
builder.Services.AddScoped<ITenantContext, TenantContext>();

// Query services
builder.Services.AddScoped<CaseQueryService>();

// Identity configuration
builder.Services.AddIdentity<AppUser, IdentityRole<Guid>>(options =>
{
    // Password settings
    options.Password.RequireDigit = true;
    options.Password.RequiredLength = 8;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = true;
    options.Password.RequireLowercase = true;
    
    // User settings
    options.User.RequireUniqueEmail = true;
    
    // Sign-in settings
    options.SignIn.RequireConfirmedEmail = false;
})
.AddEntityFrameworkStores<AnseoConnectDbContext>()
.AddDefaultTokenProviders();

// Configure dual authentication schemes
var jwtSecret = builder.Configuration["Jwt:Secret"] 
    ?? Environment.GetEnvironmentVariable("ANSEO_JWT_SECRET")
    ?? throw new InvalidOperationException("JWT secret not configured. Set 'Jwt:Secret' in appsettings.json or 'ANSEO_JWT_SECRET' environment variable.");

var jwtKey = Encoding.UTF8.GetBytes(jwtSecret);
if (jwtKey.Length < 32)
{
    throw new InvalidOperationException("JWT secret must be at least 32 bytes (256 bits) long.");
}

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"] ?? "AnseoConnect",
        ValidAudience = builder.Configuration["Jwt:Audience"] ?? "AnseoConnect",
        IssuerSigningKey = new SymmetricSecurityKey(jwtKey),
        ClockSkew = TimeSpan.Zero
    };
});
// Azure AD authentication (optional - will be added when Microsoft.Identity.Web is configured)
// For now, only local JWT authentication is enabled
// TODO: Add Entra authentication when Microsoft.Identity.Web package vulnerability is resolved

// Authorization policies
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("StaffOnly", policy =>
    {
        policy.RequireAuthenticatedUser();
        policy.RequireClaim("tenant_id");
    });
    
    options.DefaultPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
});

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

// TenantContext middleware - must come before UseAuthentication
app.UseMiddleware<TenantContextMiddleware>();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
