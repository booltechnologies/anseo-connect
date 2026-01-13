using System.Text.Json;

namespace AnseoConnect.PolicyRuntime;

/// <summary>
/// Loads reason taxonomy codes (e.g., TUSLA) from a policy pack JSON.
/// </summary>
public sealed class ReasonTaxonomyService
{
    public IReadOnlyList<ReasonCodeEntry> LoadCodes(JsonElement policyPackRoot, string countryCode = "IE")
    {
        var results = new List<ReasonCodeEntry>();

        if (!policyPackRoot.TryGetProperty("reasonTaxonomy", out var taxonomy) &&
            !policyPackRoot.TryGetProperty("reason-taxonomy", out taxonomy))
        {
            return results;
        }

        if (!taxonomy.TryGetProperty("enabled", out var enabledEl) || enabledEl.ValueKind != JsonValueKind.True)
        {
            return results;
        }

        if (!taxonomy.TryGetProperty("countryDefaults", out var defaults) ||
            !defaults.TryGetProperty(countryCode, out var country) ||
            country.ValueKind != JsonValueKind.Object)
        {
            return results;
        }

        var scheme = country.TryGetProperty("scheme", out var schemeEl) && schemeEl.ValueKind == JsonValueKind.String
            ? schemeEl.GetString()
            : "UNKNOWN";
        var version = country.TryGetProperty("version", out var verEl) && verEl.ValueKind == JsonValueKind.String
            ? verEl.GetString()
            : "UNKNOWN";

        if (country.TryGetProperty("codes", out var codes) && codes.ValueKind == JsonValueKind.Array)
        {
            foreach (var code in codes.EnumerateArray())
            {
                if (!code.TryGetProperty("code", out var cEl) || cEl.ValueKind != JsonValueKind.String)
                    continue;
                if (!code.TryGetProperty("label", out var lEl) || lEl.ValueKind != JsonValueKind.String)
                    continue;
                if (!code.TryGetProperty("type", out var tEl) || tEl.ValueKind != JsonValueKind.String)
                    continue;

                results.Add(new ReasonCodeEntry(
                    Code: cEl.GetString() ?? string.Empty,
                    Label: lEl.GetString() ?? string.Empty,
                    Type: tEl.GetString() ?? string.Empty,
                    Scheme: scheme ?? string.Empty,
                    Version: version ?? string.Empty,
                    IsDefault: true));
            }
        }

        return results;
    }
}

public sealed record ReasonCodeEntry(
    string Code,
    string Label,
    string Type,
    string Scheme,
    string Version,
    bool IsDefault);
