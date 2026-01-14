using AnseoConnect.Data.Entities;

namespace AnseoConnect.Workflow.Timeline;

/// <summary>
/// Projects AttendanceDailySummary entities into timeline events (only for significant events like absences).
/// </summary>
public sealed class AttendanceTimelineProjector : TimelineProjectorBase
{
    public override string SourceEntityType => "AttendanceDailySummary";
    public override string Category => "ATTENDANCE";
    
    public override Task<IReadOnlyList<TimelineEvent>> ProjectAsync(object sourceEntity, CancellationToken cancellationToken = default)
    {
        if (sourceEntity is not AttendanceDailySummary summary)
        {
            return Task.FromResult<IReadOnlyList<TimelineEvent>>(Array.Empty<TimelineEvent>());
        }
        
        // Only project significant attendance events (absences, not regular attendance)
        var events = new List<TimelineEvent>();
        
        var isAbsent = summary.AMStatus == "ABSENT" || summary.PMStatus == "ABSENT";
        var isAllDayAbsent = summary.AMStatus == "ABSENT" && summary.PMStatus == "ABSENT";
        
        if (isAbsent)
        {
            var title = isAllDayAbsent 
                ? "Full day absence" 
                : summary.AMStatus == "ABSENT" 
                    ? "Morning absence" 
                    : "Afternoon absence";
                    
            var summaryText = isAllDayAbsent
                ? $"Student absent all day"
                : summary.AMStatus == "ABSENT"
                    ? $"Student absent in morning"
                    : $"Student absent in afternoon";
                    
            var evt = CreateEvent(
                summary.StudentId,
                null, // Attendance events may not be linked to a case yet
                "ATTENDANCE_ABSENCE",
                summary.ComputedAtUtc,
                title: title,
                summary: summaryText,
                metadata: new 
                { 
                    summary.Date, 
                    summary.AMStatus, 
                    summary.PMStatus, 
                    summary.AMReasonCode, 
                    summary.PMReasonCode,
                    summary.ConsecutiveAbsenceDays,
                    summary.TotalAbsenceDaysYTD
                }
            );
            SetSourceEntityId(evt, summary.SummaryId);
            events.Add(evt);
        }
        
        return Task.FromResult<IReadOnlyList<TimelineEvent>>(events);
    }
}
