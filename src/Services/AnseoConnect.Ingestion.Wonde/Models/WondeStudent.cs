using System.Text.Json.Serialization;

namespace AnseoConnect.Ingestion.Wonde.Client;

/// <summary>
/// Student model from Wonde API.
/// </summary>
public sealed record WondeStudent(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("mis_id")] string? MisId,
    [property: JsonPropertyName("forename")] string? Forename,
    [property: JsonPropertyName("surname")] string? Surname,
    [property: JsonPropertyName("year_group")] WondeYearGroup? YearGroup,
    [property: JsonPropertyName("contacts")] List<WondeStudentContact>? Contacts,
    [property: JsonPropertyName("active")] bool? Active
);

public sealed record WondeYearGroup(
    [property: JsonPropertyName("name")] string? Name
);

public sealed record WondeStudentContact(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("relationship")] string? Relationship
);
