using System.Text.Json.Serialization;

namespace AnseoConnect.Ingestion.Wonde.Client;

/// <summary>
/// Class/group model from Wonde API.
/// </summary>
public sealed record WondeClass(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("name")] string? Name,
    [property: JsonPropertyName("code")] string? Code,
    [property: JsonPropertyName("academic_year")] WondeAcademicYear? AcademicYear,
    [property: JsonPropertyName("active")] bool? Active,
    [property: JsonPropertyName("students")] List<WondeClassStudent>? Students
);

public sealed record WondeAcademicYear(
    [property: JsonPropertyName("name")] string? Name
);

public sealed record WondeClassStudent(
    [property: JsonPropertyName("id")] string Id
);
