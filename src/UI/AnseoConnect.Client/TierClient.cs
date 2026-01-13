using AnseoConnect.Client.Models;
using AnseoConnect.Contracts.DTOs;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace AnseoConnect.Client;

public sealed class TierClient : ApiClientBase
{
    public TierClient(HttpClient httpClient, IOptions<ApiClientOptions> options, ILogger<TierClient> logger)
        : base(httpClient, options, logger)
    {
    }

    public async Task<List<TierDefinitionDto>> GetTierDefinitionsAsync(CancellationToken cancellationToken = default)
    {
        var result = await GetOrDefaultAsync<List<TierDefinitionDto>>("api/tiers/definitions", cancellationToken);
        return result ?? new List<TierDefinitionDto>();
    }

    public async Task<TierDefinitionDto?> GetTierDefinitionAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await GetOrDefaultAsync<TierDefinitionDto>($"api/tiers/definitions/{id}", cancellationToken);
    }

    public async Task<TierDefinitionDto?> CreateTierDefinitionAsync(TierDefinitionDto definition, CancellationToken cancellationToken = default)
    {
        return await PostOrDefaultAsync<TierDefinitionDto, TierDefinitionDto>("api/tiers/definitions", definition, cancellationToken);
    }

    public async Task<TierDefinitionDto?> UpdateTierDefinitionAsync(Guid id, TierDefinitionDto definition, CancellationToken cancellationToken = default)
    {
        return await PutOrDefaultAsync<TierDefinitionDto, TierDefinitionDto>($"api/tiers/definitions/{id}", definition, cancellationToken);
    }

    public async Task<TierAssignmentDto?> GetCurrentTierAsync(Guid caseId, CancellationToken cancellationToken = default)
    {
        return await GetOrDefaultAsync<TierAssignmentDto>($"api/cases/{caseId}/tier", cancellationToken);
    }

    public async Task<TierAssignmentDto?> AssignTierAsync(Guid caseId, int tierNumber, string? reason, string? rationale, CancellationToken cancellationToken = default)
    {
        var payload = new { TierNumber = tierNumber, Reason = reason, Rationale = rationale };
        return await PostOrDefaultAsync<object, TierAssignmentDto>($"api/cases/{caseId}/tier", payload, cancellationToken);
    }

    public async Task<List<TierAssignmentHistoryDto>> GetTierHistoryAsync(Guid caseId, CancellationToken cancellationToken = default)
    {
        var result = await GetOrDefaultAsync<List<TierAssignmentHistoryDto>>($"api/cases/{caseId}/tier/history", cancellationToken);
        return result ?? new List<TierAssignmentHistoryDto>();
    }

    public async Task<TierEvaluationResultDto?> EvaluateTierAsync(Guid caseId, CancellationToken cancellationToken = default)
    {
        return await PostOrDefaultAsync<object?, TierEvaluationResultDto>($"api/cases/{caseId}/tier/evaluate", null, cancellationToken);
    }
}

// DTOs - these would typically be in a separate DTOs file
public sealed class TierDefinitionDto
{
    public Guid TierDefinitionId { get; set; }
    public int TierNumber { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string EntryCriteriaJson { get; set; } = "{}";
    public string ExitCriteriaJson { get; set; } = "{}";
    public string EscalationCriteriaJson { get; set; } = "{}";
    public int ReviewIntervalDays { get; set; }
    public bool IsActive { get; set; }
}

public sealed class TierAssignmentDto
{
    public Guid AssignmentId { get; set; }
    public Guid CaseId { get; set; }
    public int TierNumber { get; set; }
    public string AssignmentReason { get; set; } = string.Empty;
    public string Rationale { get; set; } = string.Empty;
    public DateTimeOffset AssignedAtUtc { get; set; }
}

public sealed class TierAssignmentHistoryDto
{
    public Guid HistoryId { get; set; }
    public int FromTier { get; set; }
    public int ToTier { get; set; }
    public string ChangeType { get; set; } = string.Empty;
    public string ChangeReason { get; set; } = string.Empty;
    public DateTimeOffset ChangedAtUtc { get; set; }
}

public sealed class TierEvaluationResultDto
{
    public Guid StudentId { get; set; }
    public Guid? TierDefinitionId { get; set; }
    public bool MeetsCriteria { get; set; }
    public List<string> TriggeredConditions { get; set; } = new();
    public decimal? AttendancePercent { get; set; }
}
