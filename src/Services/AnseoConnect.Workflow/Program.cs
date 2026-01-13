using AnseoConnect.Data;
using AnseoConnect.Data.MultiTenancy;
using AnseoConnect.Data.Services;
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
builder.Services.AddSingleton<ReasonTaxonomyService>();

// Workflow services
builder.Services.AddScoped<AbsenceDetectionService>();
builder.Services.AddScoped<CaseService>();
builder.Services.AddScoped<SafeguardingService>();
builder.Services.AddScoped<TaskService>();
builder.Services.AddScoped<ReviewWindowService>();
builder.Services.AddScoped<EvidencePackService>();
builder.Services.AddScoped<NotificationRoutingService>();
builder.Services.AddScoped<AttendanceReconciliationService>();
builder.Services.AddScoped<ReasonTaxonomySyncService>();
builder.Services.AddScoped<AttendanceNormalizationService>();
builder.Services.AddScoped<InterventionRuleEngine>();
builder.Services.AddScoped<LetterGenerationService>();
builder.Services.AddScoped<MeetingService>();
builder.Services.AddScoped<InterventionAnalyticsService>();
builder.Services.AddScoped<IDistributedLockService, DistributedLockService>();
builder.Services.AddScoped<PlaybookEvaluator>();
builder.Services.AddScoped<RoiCalculatorService>();
builder.Services.AddScoped<TierEvaluator>();
builder.Services.AddScoped<MtssTierService>();
builder.Services.AddScoped<EvidencePackIntegrityService>();
builder.Services.AddScoped<EvidencePackBuilder>();
builder.Services.Configure<JobScheduleOptions>(builder.Configuration.GetSection("Jobs"));
builder.Services.AddHostedService<ScheduledReportService>();

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

builder.Services.AddHostedService<TaskDueConsumer>();
builder.Services.AddHostedService<InterventionScheduler>();

var jobsOptions = builder.Configuration.GetSection("Jobs").Get<JobScheduleOptions>() ?? new JobScheduleOptions();
if (jobsOptions.EnablePlaybooks)
{
    builder.Services.AddHostedService<PlaybookRunnerService>();
    builder.Services.AddHostedService<AutomationMetricsAggregator>();
}

builder.Services.AddHostedService<TierReviewService>();

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
