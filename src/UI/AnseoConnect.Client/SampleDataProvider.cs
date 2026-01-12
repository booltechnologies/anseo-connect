using System.Linq;
using AnseoConnect.Client.Models;
using AnseoConnect.Contracts.DTOs;

namespace AnseoConnect.Client;

public sealed class SampleDataProvider
{
    private readonly Dictionary<Guid, IReadOnlyList<GuardianContact>> _guardianDirectory;
    private readonly List<MessageSummary> _messages;
    private readonly List<MessageDetail> _messageDetails;
    private readonly List<SafeguardingAlertSummary> _alerts;
    private readonly List<GuardianContact> _missingContacts;
    private readonly List<AbsenceDto> _absences;
    private readonly List<TaskSummary> _tasks;
    private readonly List<CaseDto> _cases;
    private readonly List<ChecklistItemDto> _checklist;
    private readonly List<StudentSummary> _students;
    private readonly List<StudentProfile> _profiles;
    private readonly List<IntegrationStatusDto> _integrations;

    public IReadOnlyList<AbsenceDto> Absences => _absences;
    public IReadOnlyList<TaskSummary> Tasks => _tasks;
    public IReadOnlyList<SafeguardingAlertSummary> SafeguardingAlerts => _alerts;
    public IReadOnlyList<CaseDto> Cases => _cases;
    public IReadOnlyList<MessageSummary> Messages => _messages;
    public IReadOnlyList<MessageDetail> MessageDetails => _messageDetails;
    public IReadOnlyList<StudentSummary> Students => _students;
    public IReadOnlyList<StudentProfile> StudentProfiles => _profiles;
    public IReadOnlyList<ChecklistItemDto> Checklist => _checklist;
    public IReadOnlyList<IntegrationStatusDto> Integrations => _integrations;
    private PolicyPackAssignmentDto _policyPack;
    private SchoolSettingsDto _schoolSettings;

    public PolicyPackAssignmentDto PolicyPack => _policyPack;
    public SchoolSettingsDto SchoolSettings => _schoolSettings;
    public IReadOnlyList<GuardianContact> MissingContacts => _missingContacts;
    public IReadOnlyList<MessageSummary> Failures => _messages.Where(m => string.Equals(m.Status, "Failed", StringComparison.OrdinalIgnoreCase)).ToList();

    public SampleDataProvider()
    {
        var aoifeId = Guid.NewGuid();
        var liamId = Guid.NewGuid();
        var ellaId = Guid.NewGuid();

        var aoifeGuardians = new List<GuardianContact>
        {
            new(Guid.NewGuid(), "Mary Byrne", "Mother", "0850001111", "mary@example.com", "OPTED_IN", "OPTED_IN"),
            new(Guid.NewGuid(), "John Byrne", "Father", "", "john@example.com", "UNKNOWN", "OPTED_IN")
        };
        var liamGuardians = new List<GuardianContact>
        {
            new(Guid.NewGuid(), "Sarah Murphy", "Mother", "0850002222", "", "OPTED_IN", "UNKNOWN")
        };
        var ellaGuardians = new List<GuardianContact>
        {
            new(Guid.NewGuid(), "Claire O'Neill", "Mother", "", "", "UNKNOWN", "UNKNOWN")
        };

        _guardianDirectory = new Dictionary<Guid, IReadOnlyList<GuardianContact>>
        {
            { aoifeId, aoifeGuardians },
            { liamId, liamGuardians },
            { ellaId, ellaGuardians }
        };

        _missingContacts = new List<GuardianContact>
        {
            new(Guid.NewGuid(), "Unknown guardian", "Guardian", "", "", "UNKNOWN", "UNKNOWN"),
            new(Guid.NewGuid(), "No contact on file", "Guardian", "", "", "UNKNOWN", "UNKNOWN")
        };

        _absences = new List<AbsenceDto>
        {
            new(aoifeId, "Aoife Byrne", DateOnly.FromDateTime(DateTime.UtcNow), "AM", null),
            new(liamId, "Liam Murphy", DateOnly.FromDateTime(DateTime.UtcNow), "PM", null)
        };

        _tasks = new List<TaskSummary>
        {
            new("Call guardian for Aoife", DateTimeOffset.UtcNow.AddHours(1), "Contact", "Open", CaseId: null),
            new("Schedule meeting for Liam", DateTimeOffset.UtcNow.AddHours(3), "Tier2", "Open", CaseId: null)
        };

        _alerts = new List<SafeguardingAlertSummary>
        {
            new(Guid.NewGuid(), aoifeId, "Aoife Byrne", "HIGH", "Multiple absences flagged", DateTimeOffset.UtcNow.AddMinutes(-45), null, null)
        };

        var sampleCaseId = Guid.NewGuid();
        _cases = new List<CaseDto>
        {
            new(
                CaseId: sampleCaseId,
                StudentId: aoifeId,
                StudentName: "Aoife Byrne",
                CaseType: "ATTENDANCE",
                Tier: 2,
                Status: "OPEN",
                CreatedAtUtc: DateTimeOffset.UtcNow.AddDays(-2),
                ResolvedAtUtc: null,
                TimelineEvents: new List<CaseTimelineEventDto>
                {
                    new(Guid.NewGuid(), sampleCaseId, "CaseCreated", "Case opened from unexplained absences", DateTimeOffset.UtcNow.AddDays(-2), "system"),
                    new(Guid.NewGuid(), sampleCaseId, "MessageSent", "SMS to guardian", DateTimeOffset.UtcNow.AddHours(-6), "attendance.officer")
                })
        };

        _checklist = new List<ChecklistItemDto>
        {
            new("contact-primary", "Contact primary guardian", true, "Pending"),
            new("record-note", "Add attendance note", false, "Pending")
        };

        _messages = new List<MessageSummary>
        {
            new(Guid.NewGuid(), aoifeId, "Aoife Byrne", "SMS", "Failed", "UnexplainedAbsence", DateTimeOffset.UtcNow.AddHours(-2), "SM123", null),
            new(Guid.NewGuid(), liamId, "Liam Murphy", "Email", "Delivered", "AttendancePlan", DateTimeOffset.UtcNow.AddHours(-1), "SG456", "Tier 2 follow-up"),
            new(Guid.NewGuid(), ellaId, "Ella O'Neill", "SMS", "Queued", "SafeguardingAlert", DateTimeOffset.UtcNow.AddMinutes(-30), "SM789", null)
        };

        _messageDetails = _messages.Select(m => new MessageDetail(
            m,
            Body: $"Sample message body for {m.StudentName}. This is a stub preview to show templated content.",
            Template: m.MessageType,
            Tokens: new List<KeyValuePair<string, string>>
            {
                new("StudentName", m.StudentName),
                new("Date", DateOnly.FromDateTime(DateTime.UtcNow).ToShortDateString())
            },
            Timeline: new List<MessageTimelineEvent>
            {
                new(m.CreatedAtUtc, "Created", "Message created"),
                new(m.CreatedAtUtc.AddMinutes(1), "Sent", "Submitted to provider"),
                new(m.CreatedAtUtc.AddMinutes(5), "Delivered", "Provider acknowledged delivery")
            },
            Recipients: RecipientNames(m.StudentId),
            ConsentStatus: "OPTED_IN",
            ConsentSource: "Stub")).ToList();

        _students = new List<StudentSummary>
        {
            new(aoifeId, "Aoife Byrne", "S-1001", "Year 10", "High", 87.2),
            new(liamId, "Liam Murphy", "S-1002", "Year 11", "Medium", 91.0),
            new(ellaId, "Ella O'Neill", "S-1003", "Year 9", "Low", 95.4)
        };

        _profiles = _students.Select(s => new StudentProfile(
            s,
            GuardiansByStudent(s.StudentId).ToList(),
            _cases.Where(c => c.StudentId == s.StudentId).ToList(),
            _messages.Where(m => m.StudentId == s.StudentId).ToList())).ToList();

        _integrations = new List<IntegrationStatusDto>
        {
            new("Wonde", "Healthy", "Last sync OK", DateTimeOffset.UtcNow.AddMinutes(-10)),
            new("Sendmode", "Healthy", "Ready to send SMS", DateTimeOffset.UtcNow.AddMinutes(-5)),
            new("SendGrid", "Healthy", "Ready to send email", DateTimeOffset.UtcNow.AddMinutes(-5))
        };

        _policyPack = new PolicyPackAssignmentDto("IE-ANSEO-DEFAULT", "1.2.0", "Active");
        _schoolSettings = new SchoolSettingsDto("Europe/Dublin", "09:30", "14:00", "SMS,Email", _policyPack.Version);
    }

    public StudentProfile? FindStudent(Guid studentId) => StudentProfiles.FirstOrDefault(s => s.Summary.StudentId == studentId);
    public MessageDetail? FindMessage(Guid messageId) => MessageDetails.FirstOrDefault(m => m.Summary.MessageId == messageId);
    public CaseDto? FindCase(Guid caseId) => Cases.FirstOrDefault(c => c.CaseId == caseId);
    public IReadOnlyList<MessageSummary> MessageSummariesByStudent(Guid studentId) => Messages.Where(m => m.StudentId == studentId).ToList();
    public IReadOnlyList<GuardianContact> GuardiansByStudent(Guid studentId)
    {
        if (studentId == Guid.Empty)
        {
            return _missingContacts;
        }

        if (_guardianDirectory.TryGetValue(studentId, out var guardians))
        {
            return guardians;
        }

        return _missingContacts;
    }

    public IReadOnlyList<string> Templates { get; } = new[] { "UnexplainedAbsence", "AttendancePlan", "SafeguardingAlert", "Tier2FollowUp" };

    private IReadOnlyList<string> RecipientNames(Guid studentId) =>
        GuardiansByStudent(studentId).Select(g => g.Name).ToList();

    public MessageSummary AddMessage(MessageComposeRequest request)
    {
        var studentName = FindStudent(request.StudentId)?.Summary.Name ?? "Unknown student";
        var summary = new MessageSummary(
            Guid.NewGuid(),
            request.StudentId,
            studentName,
            request.Channel,
            "Queued",
            request.Template,
            DateTimeOffset.UtcNow,
            ProviderMessageId: null,
            Subject: null);

        _messages.Insert(0, summary);

        var recipients = new List<string>();
        foreach (var guardianId in request.GuardianIds)
        {
            var guardian = GuardiansByStudent(request.StudentId).FirstOrDefault(g => g.GuardianId == guardianId);
            if (guardian != null)
            {
                recipients.Add(guardian.Name);
            }
        }
        if (!string.IsNullOrWhiteSpace(request.OtherGuardian))
        {
            recipients.Add(request.OtherGuardian);
        }

        var detail = new MessageDetail(
            summary,
            Body: request.BodyPreview,
            Template: request.Template,
            Tokens: new List<KeyValuePair<string, string>>(),
            Timeline: new List<MessageTimelineEvent>
            {
                new(summary.CreatedAtUtc, "Created", "Message created (stub)"),
                new(summary.CreatedAtUtc.AddMinutes(1), "Queued", "Queued (stub)")
            },
            Recipients: recipients,
            ConsentStatus: "UNKNOWN",
            ConsentSource: "Stub");

        _messageDetails.Insert(0, detail);
        return summary;
    }

    public SafeguardingAlertSummary? AcknowledgeAlert(Guid alertId, string user)
    {
        var idx = _alerts.FindIndex(a => a.AlertId == alertId);
        if (idx < 0)
        {
            return null;
        }

        var updated = _alerts[idx] with { AcknowledgedBy = user, AcknowledgedAtUtc = DateTimeOffset.UtcNow };
        _alerts[idx] = updated;
        return updated;
    }

    public void UpdateSchoolSettings(SchoolSettingsDto dto)
    {
        _schoolSettings = dto;
    }

    public void UpdatePolicyPack(PolicyPackAssignmentDto dto)
    {
        _policyPack = dto;
        _schoolSettings = _schoolSettings with { PolicyPackVersion = dto.Version };
    }
}
