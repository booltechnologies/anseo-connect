using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using AnseoConnect.Client.Models;

namespace AnseoConnect.Client;

public sealed class ReportsClient : ApiClientBase
{
    public ReportsClient(HttpClient httpClient, IOptions<ApiClientOptions> options, ILogger<ReportsClient> logger)
        : base(httpClient, options, logger)
    {
    }

    public Task<List<ReportDefinitionDto>?> GetDefinitionsAsync(CancellationToken cancellationToken = default)
        => GetOrDefaultAsync<List<ReportDefinitionDto>>("api/reports/definitions", cancellationToken);

    public Task<List<ReportRunDto>?> GetRunsAsync(int take = 20, CancellationToken cancellationToken = default)
        => GetOrDefaultAsync<List<ReportRunDto>>($"api/reports/runs?take={take}", cancellationToken);

    public Task<ReportRunDto?> TriggerRunAsync(Guid definitionId, CancellationToken cancellationToken = default)
        => PostOrDefaultAsync<object, ReportRunDto>($"api/reports/definitions/{definitionId}/run", new { }, cancellationToken);

    public async Task<byte[]?> DownloadArtifactAsync(Guid artifactId, CancellationToken cancellationToken = default)
    {
        try
        {
            return await HttpClient.GetByteArrayAsync($"api/reports/artifacts/{artifactId}/download", cancellationToken);
        }
        catch
        {
            return null;
        }
    }
}

