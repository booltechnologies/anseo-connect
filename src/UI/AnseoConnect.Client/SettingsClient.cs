using AnseoConnect.Client.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AnseoConnect.Client;

public sealed class SettingsClient : ApiClientBase
{
    public SettingsClient(HttpClient httpClient, IOptions<ApiClientOptions> options, SampleDataProvider sampleData, ILogger<SettingsClient> logger)
        : base(httpClient, options, sampleData, logger)
    {
    }

    public async Task<SchoolSettingsDto> GetSchoolSettingsAsync(CancellationToken cancellationToken = default)
    {
        var result = await GetOrDefaultAsync<SchoolSettingsDto>("api/settings/school", cancellationToken);
        return result ?? SampleData.SchoolSettings;
    }

    public async Task<bool> UpdateSchoolSettingsAsync(SchoolSettingsDto dto, CancellationToken cancellationToken = default)
    {
        var ok = await PutOrFalseAsync("api/settings/school", dto, cancellationToken);
        if (!ok)
        {
            SampleData.UpdateSchoolSettings(dto);
            return true;
        }

        return ok;
    }

    public async Task<IReadOnlyList<IntegrationStatusDto>> GetIntegrationsAsync(CancellationToken cancellationToken = default)
    {
        var result = await GetOrDefaultAsync<List<IntegrationStatusDto>>("api/integrations/status", cancellationToken);
        return result ?? SampleData.Integrations;
    }

    public async Task<PolicyPackAssignmentDto> GetPolicyPackAsync(CancellationToken cancellationToken = default)
    {
        var result = await GetOrDefaultAsync<PolicyPackAssignmentDto>("api/settings/policy", cancellationToken);
        return result ?? SampleData.PolicyPack;
    }

    public async Task<bool> UpdatePolicyPackAsync(PolicyPackAssignmentDto dto, CancellationToken cancellationToken = default)
    {
        var ok = await PutOrFalseAsync("api/settings/policy", dto, cancellationToken);
        if (!ok)
        {
            SampleData.UpdatePolicyPack(dto);
            return true;
        }

        return ok;
    }
}
