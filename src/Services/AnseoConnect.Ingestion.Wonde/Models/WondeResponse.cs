using System.Text.Json.Serialization;

namespace AnseoConnect.Ingestion.Wonde.Client;

/// <summary>
/// Base response wrapper from Wonde API.
/// </summary>
public sealed record WondePagedResponse<T>(
    [property: JsonPropertyName("data")] List<T> Data,
    [property: JsonPropertyName("meta")] WondeMeta? Meta
);

public sealed record WondeMeta(
    [property: JsonPropertyName("pagination")] WondePagination? Pagination,
    [property: JsonPropertyName("includes")] List<string>? Includes
);

public sealed record WondePagination(
    [property: JsonPropertyName("next")] string? Next,
    [property: JsonPropertyName("previous")] string? Previous,
    [property: JsonPropertyName("more")] bool More,
    [property: JsonPropertyName("per_page")] int PerPage,
    [property: JsonPropertyName("current_page")] int CurrentPage
);

/// <summary>
/// School response from Wonde API.
/// </summary>
public sealed record WondeSchoolResponse(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("region")] WondeRegion? Region
);

public sealed record WondeRegion(
    [property: JsonPropertyName("domain")] string Domain
);
