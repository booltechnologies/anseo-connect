using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AnseoConnect.Client;

public abstract class ApiClientBase
{
    protected readonly HttpClient HttpClient;
    protected readonly ApiClientOptions Options;
    protected readonly SampleDataProvider SampleData;
    private readonly ILogger _logger;

    protected ApiClientBase(HttpClient httpClient, IOptions<ApiClientOptions> options, SampleDataProvider sampleData, ILogger logger)
    {
        HttpClient = httpClient;
        Options = options.Value;
        SampleData = sampleData;
        _logger = logger;
    }

    protected async Task<T?> GetOrDefaultAsync<T>(string requestUri, CancellationToken cancellationToken = default)
    {
        try
        {
            return await HttpClient.GetFromJsonAsync<T>(requestUri, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Falling back to stub data for {RequestUri}", requestUri);
            return default;
        }
    }

    protected async Task<TResponse?> PostOrDefaultAsync<TRequest, TResponse>(string requestUri, TRequest payload, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await HttpClient.PostAsJsonAsync(requestUri, payload, cancellationToken);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<TResponse>(cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Falling back to stub data for {RequestUri}", requestUri);
            return default;
        }
    }

    protected async Task<bool> PutOrFalseAsync<TRequest>(string requestUri, TRequest payload, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await HttpClient.PutAsJsonAsync(requestUri, payload, cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "PUT fallback for {RequestUri}", requestUri);
            return false;
        }
    }
}
