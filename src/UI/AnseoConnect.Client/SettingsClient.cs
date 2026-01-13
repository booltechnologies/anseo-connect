using AnseoConnect.Client.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AnseoConnect.Client;

public sealed class SettingsClient : ApiClientBase
{
    public SettingsClient(HttpClient httpClient, IOptions<ApiClientOptions> options, ILogger<SettingsClient> logger)
        : base(httpClient, options, logger)
    {
    }

    public async Task<SchoolSettingsDto> GetSchoolSettingsAsync(CancellationToken cancellationToken = default)
    {
        var result = await GetOrDefaultAsync<SchoolSettingsDto>("api/settings/school", cancellationToken);
        return result ?? new SchoolSettingsDto();
    }

    public async Task<bool> UpdateSchoolSettingsAsync(SchoolSettingsDto dto, CancellationToken cancellationToken = default)
    {
        return await PutOrFalseAsync("api/settings/school", dto, cancellationToken);
    }

    public async Task<IReadOnlyList<IntegrationStatusDto>> GetIntegrationsAsync(CancellationToken cancellationToken = default)
    {
        var result = await GetOrDefaultAsync<List<IntegrationStatusDto>>("api/integrations/status", cancellationToken);
        return result ?? new List<IntegrationStatusDto>();
    }

    public async Task<PolicyPackAssignmentDto> GetPolicyPackAsync(CancellationToken cancellationToken = default)
    {
        var result = await GetOrDefaultAsync<PolicyPackAssignmentDto>("api/settings/policy", cancellationToken);
        return result ?? new PolicyPackAssignmentDto(string.Empty, string.Empty, null);
    }

    public async Task<bool> UpdatePolicyPackAsync(PolicyPackAssignmentDto dto, CancellationToken cancellationToken = default)
    {
        return await PutOrFalseAsync("api/settings/policy", dto, cancellationToken);
    }
}
