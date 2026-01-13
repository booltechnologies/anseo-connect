using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using AnseoConnect.Client.Models;

namespace AnseoConnect.Client;

public sealed class InterventionsClient : ApiClientBase
{
    public InterventionsClient(HttpClient httpClient, IOptions<ApiClientOptions> options, ILogger<InterventionsClient> logger)
        : base(httpClient, options, logger)
    {
    }

    public Task<List<EligibleStudentDto>?> GetQueueAsync(CancellationToken cancellationToken = default)
        => GetOrDefaultAsync<List<EligibleStudentDto>>("api/interventions/queue", cancellationToken);

    public Task<List<InterventionRuleSetDto>?> GetRuleSetsAsync(CancellationToken cancellationToken = default)
        => GetOrDefaultAsync<List<InterventionRuleSetDto>>("api/interventions/rules", cancellationToken);

    public Task<StudentInterventionInstanceDto?> CreateInstanceAsync(Guid studentId, Guid ruleSetId, CancellationToken cancellationToken = default)
        => PostOrDefaultAsync<CreateInstanceRequest, StudentInterventionInstanceDto>("api/interventions/instances", new CreateInstanceRequest(studentId, ruleSetId), cancellationToken);

    public Task<StudentInterventionInstanceDto?> AdvanceStageAsync(Guid instanceId, CancellationToken cancellationToken = default)
        => PostOrDefaultAsync<object, StudentInterventionInstanceDto>($"api/interventions/instances/{instanceId}/advance", new { }, cancellationToken);

    public Task<InterventionInstanceDetailDto?> GetInstanceAsync(Guid instanceId, CancellationToken cancellationToken = default)
        => GetOrDefaultAsync<InterventionInstanceDetailDto>($"api/interventions/instances/{instanceId}", cancellationToken);

    private sealed record CreateInstanceRequest(Guid StudentId, Guid RuleSetId);
}

