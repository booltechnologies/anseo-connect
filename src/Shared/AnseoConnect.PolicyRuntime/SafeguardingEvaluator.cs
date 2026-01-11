using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace AnseoConnect.PolicyRuntime;

/// <summary>
/// Implementation of ISafeguardingEvaluator using policy pack logic.
/// Reuses the same logic as PolicyPackTool.ComputeSafeguarding.
/// </summary>
public sealed class SafeguardingEvaluator : ISafeguardingEvaluator
{
    private readonly ILogger<SafeguardingEvaluator>? _logger;

    public SafeguardingEvaluator(ILogger<SafeguardingEvaluator>? logger = null)
    {
        _logger = logger;
    }

    public SafeguardingEvaluationResult Evaluate(JsonElement policyPackRoot, Dictionary<string, object> metrics)
    {
        bool createAlert = false;
        string? severity = null;
        string caseType = "SAFEGUARDING";
        string? checklistId = null;

        if (!policyPackRoot.TryGetProperty("safeguarding", out var sg))
        {
            return new SafeguardingEvaluationResult { CreateAlert = false };
        }

        if (sg.TryGetProperty("restrictedCaseType", out var rct) && rct.ValueKind == JsonValueKind.String)
        {
            caseType = rct.GetString() ?? caseType;
        }

        // Convert metrics to JSON element for comparison
        var metricsJson = JsonSerializer.Serialize(metrics);
        var metricsDoc = JsonDocument.Parse(metricsJson);
        var metricsRoot = metricsDoc.RootElement;

        // Evaluate patternTriggers
        if (sg.TryGetProperty("patternTriggers", out var pt) && pt.ValueKind == JsonValueKind.Array)
        {
            foreach (var trig in pt.EnumerateArray())
            {
                if (!trig.TryGetProperty("whenAll", out var whenAll) || whenAll.ValueKind != JsonValueKind.Array)
                    continue;

                bool all = true;

                foreach (var cond in whenAll.EnumerateArray())
                {
                    if (!cond.TryGetProperty("metric", out var metricEl) || metricEl.ValueKind != JsonValueKind.String)
                    {
                        all = false;
                        break;
                    }
                    if (!cond.TryGetProperty("op", out var opEl) || opEl.ValueKind != JsonValueKind.String)
                    {
                        all = false;
                        break;
                    }
                    if (!cond.TryGetProperty("value", out var valEl))
                    {
                        all = false;
                        break;
                    }

                    var metric = metricEl.GetString()!;
                    var op = opEl.GetString()!;

                    if (!metricsRoot.TryGetProperty(metric, out var actualEl))
                    {
                        all = false;
                        break;
                    }

                    if (!Compare(actualEl, op, valEl))
                    {
                        all = false;
                        break;
                    }
                }

                if (all)
                {
                    createAlert = true;

                    if (trig.TryGetProperty("action", out var action) &&
                        action.TryGetProperty("severity", out var sev) &&
                        sev.ValueKind == JsonValueKind.String)
                    {
                        severity = sev.GetString();
                    }

                    break;
                }
            }
        }

        if (!createAlert)
        {
            return new SafeguardingEvaluationResult { CreateAlert = false };
        }

        // Map severity -> checklistId from safeguarding.playbook.checklists
        if (!string.IsNullOrWhiteSpace(severity) &&
            sg.TryGetProperty("playbook", out var pb) &&
            pb.TryGetProperty("checklists", out var cls) &&
            cls.ValueKind == JsonValueKind.Array)
        {
            foreach (var cl in cls.EnumerateArray())
            {
                if (cl.TryGetProperty("severity", out var s) &&
                    s.ValueKind == JsonValueKind.String &&
                    s.GetString()!.Equals(severity, StringComparison.OrdinalIgnoreCase) &&
                    cl.TryGetProperty("checklistId", out var id) &&
                    id.ValueKind == JsonValueKind.String)
                {
                    checklistId = id.GetString();
                    break;
                }
            }
        }

        return new SafeguardingEvaluationResult
        {
            CreateAlert = true,
            Severity = severity ?? "MEDIUM",
            ChecklistId = checklistId
        };
    }

    private static bool Compare(JsonElement actual, string op, JsonElement expected)
    {
        if (TryGetNumber(actual, out var a) && TryGetNumber(expected, out var b))
        {
            return op switch
            {
                ">=" => a >= b,
                ">" => a > b,
                "<=" => a <= b,
                "<" => a < b,
                "==" => Math.Abs(a - b) < 0.0001, // Floating point comparison
                "!=" => Math.Abs(a - b) >= 0.0001,
                _ => false
            };
        }

        if (actual.ValueKind == JsonValueKind.String && expected.ValueKind == JsonValueKind.String)
        {
            var s1 = actual.GetString();
            var s2 = expected.GetString();
            return op switch
            {
                "==" => s1 == s2,
                "!=" => s1 != s2,
                _ => false
            };
        }

        return false;
    }

    private static bool TryGetNumber(JsonElement el, out double value)
    {
        value = 0;
        if (el.ValueKind == JsonValueKind.Number)
        {
            return el.TryGetDouble(out value);
        }
        if (el.ValueKind == JsonValueKind.String && double.TryParse(el.GetString(), out var d))
        {
            value = d;
            return true;
        }
        return false;
    }
}
