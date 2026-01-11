using AnseoConnect.Data;
using AnseoConnect.Data.MultiTenancy;
using AnseoConnect.Ingestion.Wonde.Client;
using AnseoConnect.Ingestion.Wonde.Services;
using AnseoConnect.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Http;
using System.Net.Http.Headers;

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

// TenantContext - scoped per request
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

// Wonde API configuration
var wondeToken = builder.Configuration["Wonde:Token"]
    ?? Environment.GetEnvironmentVariable("WONDE_TOKEN")
    ?? throw new InvalidOperationException("Wonde token not found. Set 'Wonde:Token' in appsettings.json or 'WONDE_TOKEN' environment variable.");

var wondeDefaultDomain = builder.Configuration["Wonde:DefaultDomain"]
    ?? Environment.GetEnvironmentVariable("WONDE_DEFAULT_DOMAIN")
    ?? "api.wonde.com";

// Configure HttpClient for WondeClient using IHttpClientFactory
builder.Services.AddHttpClient("Wonde", client =>
{
    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", wondeToken);
    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
    client.Timeout = TimeSpan.FromMinutes(5);
});

builder.Services.AddScoped<IWondeClient>(sp =>
{
    var httpClientFactory = sp.GetRequiredService<IHttpClientFactory>();
    var httpClient = httpClientFactory.CreateClient("Wonde");
    var logger = sp.GetRequiredService<ILogger<WondeClient>>();
    return new WondeClient(httpClient, wondeToken, wondeDefaultDomain, logger, disposeHttpClient: false);
});

builder.Services.AddScoped<IngestionService>();

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
