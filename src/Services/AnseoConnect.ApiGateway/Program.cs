using AnseoConnect.Data;
using AnseoConnect.Data.Entities;
using AnseoConnect.Data.MultiTenancy;
using AnseoConnect.Data.Services;
using AnseoConnect.ApiGateway.Authorization;
using AnseoConnect.ApiGateway.Health;
using AnseoConnect.ApiGateway.Middleware;
using AnseoConnect.ApiGateway.Services;
using AnseoConnect.Shared;
using AnseoConnect.Workflow.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Identity.Web;
using Microsoft.IdentityModel.Tokens;
using System.Linq;
using System.Text;
using AnseoConnect.ApiGateway.Hubs;

var builder = WebApplication.CreateBuilder(args);

var authSchemes = new[] { "LocalBearer", JwtBearerDefaults.AuthenticationScheme };

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddSignalR();
builder.Services.AddMemoryCache();

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
builder.Services.AddScoped<ReportingService>();
builder.Services.AddScoped<IPermissionService, PermissionService>();
builder.Services.AddScoped<ReasonTaxonomySyncService>();
builder.Services.AddScoped<InterventionRuleEngine>();
builder.Services.AddScoped<LetterGenerationService>();
builder.Services.AddScoped<MeetingService>();
builder.Services.AddScoped<InterventionAnalyticsService>();
builder.Services.AddScoped<IDistributedLockService, DistributedLockService>();
builder.Services.AddScoped<IAuditService, AuditService>();
builder.Services.AddScoped<RoiCalculatorService>();
builder.Services.AddScoped<TierEvaluator>();
builder.Services.AddScoped<MtssTierService>();
builder.Services.AddScoped<EvidencePackIntegrityService>();
builder.Services.AddScoped<EvidencePackBuilder>();
builder.Services.AddScoped<CaseService>();
builder.Services.AddScoped<TimelineService>();
builder.Services.AddSingleton<NotificationBroadcaster>();
// Email/SMS senders for guardian auth
var sendGridKey = builder.Configuration["SendGrid:ApiKey"] ?? Environment.GetEnvironmentVariable("SENDGRID_API_KEY");
var sendGridFromEmail = builder.Configuration["SendGrid:FromEmail"] ?? Environment.GetEnvironmentVariable("SENDGRID_FROM_EMAIL");
var sendGridFromName = builder.Configuration["SendGrid:FromName"] ?? Environment.GetEnvironmentVariable("SENDGRID_FROM_NAME") ?? "Anseo Connect";
if (!string.IsNullOrWhiteSpace(sendGridKey) && !string.IsNullOrWhiteSpace(sendGridFromEmail))
{
    builder.Services.AddSingleton<IEmailSender>(sp =>
        new EmailSender(sendGridKey, sendGridFromEmail!, sendGridFromName, sp.GetRequiredService<ILogger<EmailSender>>()));
}

var sendmodeUsername = builder.Configuration["Sendmode:Username"]
    ?? builder.Configuration["Sendmode:ApiKey"]
    ?? Environment.GetEnvironmentVariable("SENDMODE_USERNAME")
    ?? Environment.GetEnvironmentVariable("SENDMODE_API_KEY");
var sendmodePassword = builder.Configuration["Sendmode:Password"]
    ?? builder.Configuration["Sendmode:ApiKey"]
    ?? Environment.GetEnvironmentVariable("SENDMODE_PASSWORD")
    ?? Environment.GetEnvironmentVariable("SENDMODE_API_KEY");
var sendmodeApiUrl = builder.Configuration["Sendmode:ApiUrl"]
    ?? Environment.GetEnvironmentVariable("SENDMODE_API_URL");
var sendmodeFrom = builder.Configuration["Sendmode:FromNumber"]
    ?? Environment.GetEnvironmentVariable("SENDMODE_FROM_NUMBER");
if (!string.IsNullOrWhiteSpace(sendmodeUsername) && !string.IsNullOrWhiteSpace(sendmodePassword))
{
    builder.Services.AddHttpClient<SendmodeSmsSender>(client =>
    {
        client.Timeout = TimeSpan.FromSeconds(15);
    });
    builder.Services.AddSingleton<ISmsSender>(sp =>
        sp.GetRequiredService<SendmodeSmsSender>());
}

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
    options.DefaultAuthenticateScheme = "LocalBearer";
    options.DefaultChallengeScheme = "LocalBearer";
})
.AddJwtBearer("LocalBearer", options =>
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
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            if (string.IsNullOrEmpty(context.Token) && context.Request.Cookies.TryGetValue("guardian_auth", out var cookieToken))
            {
                context.Token = cookieToken;
            }
            return Task.CompletedTask;
        }
    };
})
.AddMicrosoftIdentityWebApi(builder.Configuration.GetSection("AzureAd"));

builder.Services.Configure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, options =>
{
    options.TokenValidationParameters.ClockSkew = TimeSpan.Zero;
    options.TokenValidationParameters.NameClaimType = "preferred_username";
    options.Events ??= new JwtBearerEvents();
    options.Events.OnTokenValidated = context =>
    {
        var tid = context.Principal?.FindFirst("tid")?.Value;
        if (!string.IsNullOrWhiteSpace(tid))
        {
            context.Principal!.Identities.First().AddClaim(new System.Security.Claims.Claim("tenant_id", tid));
        }

        var school = context.Principal?.FindFirst("school_id")?.Value;
        if (!string.IsNullOrWhiteSpace(school))
        {
            context.Principal!.Identities.First().AddClaim(new System.Security.Claims.Claim("school_id", school));
        }

        return Task.CompletedTask;
    };
});

// Authorization policies
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("StaffOnly", policy =>
    {
        policy.AddAuthenticationSchemes(authSchemes);
        policy.RequireAuthenticatedUser();
        policy.RequireClaim("tenant_id");
    });

    options.AddPolicy("AttendanceAccess", policy =>
    {
        policy.AddAuthenticationSchemes(authSchemes);
        policy.RequireRole("AttendanceAdmin", "YearHead", "Principal", "DeputyPrincipal", "DLP");
    });

    options.AddPolicy("CaseManagement", policy =>
    {
        policy.AddAuthenticationSchemes(authSchemes);
        policy.RequireRole("YearHead", "Principal", "DeputyPrincipal", "DLP");
    });

    options.AddPolicy("SafeguardingAccess", policy =>
    {
        policy.AddAuthenticationSchemes(authSchemes);
        policy.RequireRole("DLP", "Principal", "DeputyPrincipal");
    });

    options.AddPolicy("ReportingAccess", policy =>
    {
        policy.AddAuthenticationSchemes(authSchemes);
        policy.RequireRole("Principal", "DeputyPrincipal", "ETBTrustAdmin");
    });

    options.AddPolicy("ETBTrustAccess", policy =>
    {
        policy.AddAuthenticationSchemes(authSchemes);
        policy.RequireRole("ETBTrustAdmin");
    });

    options.AddPolicy("SettingsAdmin", policy =>
    {
        policy.AddAuthenticationSchemes(authSchemes);
        policy.RequireRole("Principal", "DeputyPrincipal");
    });

    options.AddPolicy("TierManagement", policy =>
    {
        policy.AddAuthenticationSchemes(authSchemes);
        policy.RequireRole("Principal", "DeputyPrincipal", "ETBTrustAdmin");
    });

    options.AddPolicy("EvidenceExport", policy =>
    {
        policy.AddAuthenticationSchemes(authSchemes);
        policy.RequireRole("YearHead", "Principal", "DeputyPrincipal", "DLP");
    });
    
    options.DefaultPolicy = new AuthorizationPolicyBuilder()
        .AddAuthenticationSchemes(authSchemes)
        .RequireAuthenticatedUser()
        .Build();
});

// Authorization handlers
builder.Services.AddScoped<IAuthorizationHandler, PermissionHandler>();

// Background services
builder.Services.AddHostedService<AlertEvaluationService>();

// Health checks
builder.Services.AddHealthChecks()
    .AddCheck<OutboxHealthCheck>("outbox")
    .AddCheck<DeliverabilityHealthCheck>("deliverability");

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
app.MapHub<NotificationsHub>("/hubs/notifications");

// Health check endpoints
app.MapHealthChecks("/health");
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready")
});
app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = _ => false // Liveness probe - minimal check
});

app.Run();
