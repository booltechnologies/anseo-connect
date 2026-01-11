using System.Text.Json.Serialization;

namespace AnseoConnect.Ingestion.Wonde.Client;

/// <summary>
/// Attendance mark model from Wonde API.
/// </summary>
public sealed record WondeAttendance(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("student_id")] string StudentId,
    [property: JsonPropertyName("date")] WondeDate? Date,
    [property: JsonPropertyName("period")] WondePeriod? Period,
    [property: JsonPropertyName("status")] string? Status,
    [property: JsonPropertyName("absence")] WondeAbsence? Absence
);

public sealed record WondeDate(
    [property: JsonPropertyName("date")] string DateString,
    [property: JsonPropertyName("timezone")] string? Timezone
);

public sealed record WondePeriod(
    [property: JsonPropertyName("id")] string? Id,
    [property: JsonPropertyName("name")] string? Name,
    [property: JsonPropertyName("code")] string? Code
);

public sealed record WondeAbsence(
    [property: JsonPropertyName("reason")] WondeAbsenceReason? Reason
);

public sealed record WondeAbsenceReason(
    [property: JsonPropertyName("code")] string? Code
);

/// <summary>
/// Student absence record model from Wonde API.
/// </summary>
public sealed record WondeStudentAbsence(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("student_id")] string StudentId,
    [property: JsonPropertyName("start_date")] WondeDate? StartDate,
    [property: JsonPropertyName("end_date")] WondeDate? EndDate,
    [property: JsonPropertyName("absence_type")] string? AbsenceType,
    [property: JsonPropertyName("reason")] string? Reason
);
