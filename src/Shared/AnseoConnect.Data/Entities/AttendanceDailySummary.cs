using AnseoConnect.Data.MultiTenancy;

namespace AnseoConnect.Data.Entities;

/// <summary>
/// Aggregated daily attendance summary per student (normalized AM/PM and rolling metrics).
/// </summary>
public sealed class AttendanceDailySummary : SchoolEntity
{
    public Guid SummaryId { get; set; }
    public Guid StudentId { get; set; }
    public DateOnly Date { get; set; }

    public string AMStatus { get; set; } = "UNKNOWN";
    public string PMStatus { get; set; } = "UNKNOWN";
    public string? AMReasonCode { get; set; }
    public string? PMReasonCode { get; set; }

    public decimal AttendancePercent { get; set; }
    public int ConsecutiveAbsenceDays { get; set; }
    public int TotalAbsenceDaysYTD { get; set; }
    public DateTimeOffset ComputedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    public Student? Student { get; set; }
}

