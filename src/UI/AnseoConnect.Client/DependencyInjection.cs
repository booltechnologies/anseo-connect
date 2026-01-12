using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace AnseoConnect.Client;

public static class DependencyInjection
{
    public static IServiceCollection AddAnseoConnectApiClients(
        this IServiceCollection services,
        Action<ApiClientOptions>? configureOptions = null)
    {
        services.Configure(configureOptions ?? (_ => { }));

        services.AddScoped<SampleDataProvider>();
        services.AddScoped<BearerTokenHandler>();

        services.AddHttpClient(HttpClientName, (sp, client) =>
        {
            var options = sp.GetRequiredService<IOptions<ApiClientOptions>>().Value;
            client.BaseAddress = options.BaseAddress;
        }).AddHttpMessageHandler<BearerTokenHandler>();

        services.AddScoped<AuthClient>();
        services.AddScoped<TodayClient>();
        services.AddScoped<CasesClient>();
        services.AddScoped<MessagesClient>();
        services.AddScoped<StudentsClient>();
        services.AddScoped<SettingsClient>();
        services.AddScoped<SafeguardingClient>();
        services.AddScoped<ConsentClient>();

        return services;
    }

    public const string HttpClientName = "AnseoConnect.Api";
}
