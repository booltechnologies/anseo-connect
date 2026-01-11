using AnseoConnect.Data;
using AnseoConnect.Data.MultiTenancy;
using AnseoConnect.PolicyRuntime;
using AnseoConnect.Shared;
using AnseoConnect.Workflow.Consumers;
using AnseoConnect.Workflow.Services;
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

// Policy runtime
builder.Services.AddSingleton<ISafeguardingEvaluator, SafeguardingEvaluator>();

// Workflow services
builder.Services.AddScoped<AbsenceDetectionService>();
builder.Services.AddScoped<CaseService>();
builder.Services.AddScoped<SafeguardingService>();

// Register consumers as hosted services
builder.Services.AddHostedService<AttendanceMarksIngestedConsumer>(sp =>
{
    var logger = sp.GetRequiredService<ILogger<AttendanceMarksIngestedConsumer>>();
    return new AttendanceMarksIngestedConsumer(serviceBusConnectionString, sp, logger);
});

builder.Services.AddHostedService<MessageEventConsumer>(sp =>
{
    var logger = sp.GetRequiredService<ILogger<MessageEventConsumer>>();
    return new MessageEventConsumer(serviceBusConnectionString, sp, logger);
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
