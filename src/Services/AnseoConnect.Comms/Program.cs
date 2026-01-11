using AnseoConnect.Comms.Consumers;
using AnseoConnect.Comms.Services;
using AnseoConnect.Data;
using AnseoConnect.Data.MultiTenancy;
using AnseoConnect.PolicyRuntime;
using AnseoConnect.Shared;
using Microsoft.EntityFrameworkCore;

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

// TenantContext - scoped per request/message
builder.Services.AddScoped<ITenantContext, TenantContext>();

// Service Bus configuration
var serviceBusConnectionString = builder.Configuration.GetConnectionString("ServiceBus")
    ?? Environment.GetEnvironmentVariable("ANSEO_SERVICEBUS")
    ?? throw new InvalidOperationException("Service Bus connection string not found. Set 'ServiceBus' connection string or 'ANSEO_SERVICEBUS' environment variable.");

builder.Services.AddSingleton<IMessageBus>(sp =>
{
    var logger = sp.GetRequiredService<ILogger<ServiceBusMessageBus>>();
    return new ServiceBusMessageBus(serviceBusConnectionString, logger);
});

// Sendmode SMS configuration
var sendmodeUsername = builder.Configuration["Sendmode:Username"]
    ?? Environment.GetEnvironmentVariable("SENDMODE_USERNAME");
var sendmodeApiKey = builder.Configuration["Sendmode:ApiKey"]
    ?? Environment.GetEnvironmentVariable("SENDMODE_API_KEY");
var sendmodePassword = builder.Configuration["Sendmode:Password"]
    ?? Environment.GetEnvironmentVariable("SENDMODE_PASSWORD");
var sendmodeApiUrl = builder.Configuration["Sendmode:ApiUrl"]
    ?? Environment.GetEnvironmentVariable("SENDMODE_API_URL")
    ?? "https://api.sendmode.com/v2/sendSMS";
var sendmodeFromNumber = builder.Configuration["Sendmode:FromNumber"]
    ?? Environment.GetEnvironmentVariable("SENDMODE_FROM_NUMBER"); // Optional

if (string.IsNullOrWhiteSpace(sendmodeUsername) && string.IsNullOrWhiteSpace(sendmodeApiKey))
{
    throw new InvalidOperationException("Sendmode Username or ApiKey not found. Set 'Sendmode:Username' or 'Sendmode:ApiKey' in appsettings.json or environment variables.");
}

if (string.IsNullOrWhiteSpace(sendmodePassword) && string.IsNullOrWhiteSpace(sendmodeApiKey))
{
    throw new InvalidOperationException("Sendmode Password or ApiKey not found. Set 'Sendmode:Password' (for username/password) or 'Sendmode:ApiKey' (for API key auth) in appsettings.json or environment variables.");
}

// Register HttpClient for Sendmode (no base address - will use full URL in SendmodeSender)
builder.Services.AddHttpClient<SendmodeSender>(client =>
{
    // Don't set BaseAddress - SendmodeSender will use full URL from apiUrl parameter
    client.Timeout = TimeSpan.FromSeconds(30);
});

builder.Services.AddSingleton<SendmodeSender>(sp =>
{
    var httpClientFactory = sp.GetRequiredService<IHttpClientFactory>();
    var httpClient = httpClientFactory.CreateClient(nameof(SendmodeSender));
    var logger = sp.GetRequiredService<ILogger<SendmodeSender>>();
    
    // Use API key if provided, otherwise use username/password
    // If using API key auth, both username and password will be the API key
    var username = sendmodeApiKey ?? sendmodeUsername!;
    var password = sendmodeApiKey ?? sendmodePassword!;
    
    return new SendmodeSender(httpClient, username, password, sendmodeFromNumber, logger, sendmodeApiUrl);
});

// SendGrid Email configuration (optional - only if email support is needed)
var sendGridApiKey = builder.Configuration["SendGrid:ApiKey"]
    ?? Environment.GetEnvironmentVariable("SENDGRID_API_KEY");
var sendGridFromEmail = builder.Configuration["SendGrid:FromEmail"]
    ?? Environment.GetEnvironmentVariable("SENDGRID_FROM_EMAIL");
var sendGridFromName = builder.Configuration["SendGrid:FromName"]
    ?? Environment.GetEnvironmentVariable("SENDGRID_FROM_NAME")
    ?? "Anseo Connect";

if (!string.IsNullOrWhiteSpace(sendGridApiKey) && !string.IsNullOrWhiteSpace(sendGridFromEmail))
{
    builder.Services.AddSingleton<SendGridEmailSender>(sp =>
    {
        var logger = sp.GetRequiredService<ILogger<SendGridEmailSender>>();
        return new SendGridEmailSender(sendGridApiKey, sendGridFromEmail, sendGridFromName, logger);
    });
}
// If SendGrid is not configured, MessageService will handle null gracefully

// Policy runtime
builder.Services.AddSingleton<IConsentEvaluator, ConsentEvaluator>();

// Message service
builder.Services.AddScoped<MessageService>();

// Register consumer as hosted service
builder.Services.AddHostedService<SendMessageRequestedConsumer>(sp =>
{
    var logger = sp.GetRequiredService<ILogger<SendMessageRequestedConsumer>>();
    return new SendMessageRequestedConsumer(serviceBusConnectionString, sp, logger);
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
