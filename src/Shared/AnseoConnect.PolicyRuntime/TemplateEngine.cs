using System.Text.Json;
using System.Text.RegularExpressions;

namespace AnseoConnect.PolicyRuntime;

/// <summary>
/// Simple template engine for channel-specific templates with {{Variable}} substitution and basic tone constraint metadata.
/// </summary>
public sealed class TemplateEngine
{
    private static readonly Regex TokenRegex = new("{{(.*?)}}", RegexOptions.Compiled);

    public MessageTemplateResult Render(JsonElement policyPackRoot, string templateId, Dictionary<string, string> data)
    {
        if (!policyPackRoot.TryGetProperty("templates", out var templates) || templates.ValueKind != JsonValueKind.Array)
        {
            throw new InvalidOperationException("Policy pack missing templates.");
        }

        var template = templates
            .EnumerateArray()
            .FirstOrDefault(t => t.TryGetProperty("id", out var idEl) && idEl.GetString() == templateId);

        if (template.ValueKind == JsonValueKind.Undefined)
        {
            throw new InvalidOperationException($"Template {templateId} not found.");
        }

        string channel = template.GetProperty("channel").GetString() ?? "SMS";
        string? subject = template.TryGetProperty("subject", out var subj) && subj.ValueKind != JsonValueKind.Null ? subj.GetString() : null;
        string body = template.GetProperty("body").GetString() ?? string.Empty;
        int? maxLength = template.TryGetProperty("maxLength", out var maxEl) && maxEl.ValueKind == JsonValueKind.Number ? maxEl.GetInt32() : null;

        var renderedSubject = subject != null ? ReplaceTokens(subject, data) : null;
        var renderedBody = ReplaceTokens(body, data);

        if (maxLength.HasValue && renderedBody.Length > maxLength.Value)
        {
            renderedBody = renderedBody.Substring(0, maxLength.Value);
        }

        return new MessageTemplateResult(channel, renderedSubject, renderedBody);
    }

    private static string ReplaceTokens(string text, Dictionary<string, string> data)
    {
        return TokenRegex.Replace(text, match =>
        {
            var key = match.Groups[1].Value.Trim();
            return data.TryGetValue(key, out var value) ? value : string.Empty;
        });
    }
}

public sealed record MessageTemplateResult(string Channel, string? Subject, string Body);
