using AnseoConnect.Client.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AnseoConnect.Client;

public sealed class IngestionHealthClient : ApiClientBase
{
    private readonly ILogger<IngestionHealthClient> _logger;

    public IngestionHealthClient(HttpClient httpClient, IOptions<ApiClientOptions> options, ILogger<IngestionHealthClient> logger)
        : base(httpClient, options, logger)
    {
        _logger = logger;
    }

    public async Task<IReadOnlyList<IngestionHealthDto>> GetHealthAsync(CancellationToken cancellationToken = default)
    {
        var response = await GetOrDefaultAsync<IReadOnlyList<IngestionHealthDto>>("api/ingestion/health", cancellationToken);
        if (response != null)
        {
            return response;
        }

        _logger.LogWarning("Ingestion health unavailable, returning empty set");
        return Array.Empty<IngestionHealthDto>();
    }

    public async Task<bool> ResumeAsync(Guid schoolId, CancellationToken cancellationToken = default)
    {
        return await PutOrFalseAsync<object>($"api/ingestion/health/{schoolId}/resume", new { }, cancellationToken);
    }
}
