using AnseoConnect.Data.MultiTenancy;

namespace AnseoConnect.Data.Entities;

/// <summary>
/// Execution instance of a report definition.
/// </summary>
public sealed class ReportRun : SchoolEntity
{
    public Guid RunId { get; set; }
    public Guid DefinitionId { get; set; }
    public DateTimeOffset StartedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? CompletedAtUtc { get; set; }
    public string Status { get; set; } = "RUNNING"; // RUNNING, COMPLETED, FAILED
    public string? ErrorMessage { get; set; }
}

