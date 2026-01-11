using System.Text.Json.Serialization;

namespace AnseoConnect.Ingestion.Wonde.Client;

/// <summary>
/// Contact (guardian) model from Wonde API.
/// </summary>
public sealed record WondeContact(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("mis_id")] string? MisId,
    [property: JsonPropertyName("forename")] string? Forename,
    [property: JsonPropertyName("surname")] string? Surname,
    [property: JsonPropertyName("contact_details")] List<WondeContactDetail>? ContactDetails
);

public sealed record WondeContactDetail(
    [property: JsonPropertyName("type")] string Type,
    [property: JsonPropertyName("value")] string Value
);
