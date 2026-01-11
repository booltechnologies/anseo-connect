using System.Text.Json;
using Json.Schema;

static int Fail(string msg)
{
    Console.Error.WriteLine(msg);
    return 1;
}

if (args.Length < 2)
    return Fail("Usage: PolicyPackTool <validate|test> <path>");

var command = args[0].ToLowerInvariant();
var targetPath = args[1];

if (!Directory.Exists(targetPath) && !File.Exists(targetPath))
    return Fail($"Path not found: {targetPath}");

var repoRoot = Directory.GetCurrentDirectory();

// --------------------
// Load and register schemas (for validate)
// --------------------
JsonSchema? rootSchema = null;
var schemaDir = Path.Combine(repoRoot, "policy-packs", "schema");
if (Directory.Exists(schemaDir))
{
    var schemaFiles = Directory.EnumerateFiles(schemaDir, "*.schema.json", SearchOption.TopDirectoryOnly).ToList();
    foreach (var sf in schemaFiles)
    {
        var schemaText = await File.ReadAllTextAsync(sf);
        var schema = JsonSchema.FromText(schemaText);
        SchemaRegistry.Global.Register(schema);

        if (Path.GetFileName(sf).Equals("policy-pack.schema.json", StringComparison.OrdinalIgnoreCase))
            rootSchema = schema;
    }
}

// Find JSON files
IEnumerable<string> jsonFiles =
    File.Exists(targetPath)
        ? new[] { targetPath }
        : Directory.EnumerateFiles(targetPath, "*.json", SearchOption.AllDirectories);

jsonFiles = jsonFiles.Where(f => !f.Contains(Path.Combine("policy-packs", "schema"), StringComparison.OrdinalIgnoreCase));

return command switch
{
    "validate" => await RunValidate(jsonFiles, rootSchema),
    "test" => await RunTests(jsonFiles),
    _ => Fail("Unknown command. Usage: PolicyPackTool <validate|test> <path>")
};

// --------------------
// VALIDATE
// --------------------
static async Task<int> RunValidate(IEnumerable<string> jsonFiles, JsonSchema? rootSchema)
{
    if (rootSchema is null)
        return Fail("Root schema not loaded. Ensure policy-packs/schema/policy-pack.schema.json exists.");

    var options = new EvaluationOptions
    {
        OutputFormat = OutputFormat.List,
        RequireFormatValidation = false
    };

    int errors = 0;

    foreach (var file in jsonFiles)
    {
        if (!TryReadJson(file, out var doc, out var err))
        {
            errors++;
            Console.Error.WriteLine(err);
            continue;
        }

        var result = rootSchema.Evaluate(doc!.RootElement, options);
        if (!result.IsValid)
        {
            errors++;
            Console.Error.WriteLine($"\n[SCHEMA FAIL] {file}");
            foreach (var (instance, keyword, message) in EnumerateErrors(result))
                Console.Error.WriteLine($" - {instance} [{keyword}] {message}");
        }
        else
        {
            Console.WriteLine($"[OK] {file}");
        }
    }

    Console.WriteLine($"\nValidation complete. Errors: {errors}");
    return errors == 0 ? 0 : 1;
}

static IEnumerable<(string InstanceLocation, string Keyword, string Message)> EnumerateErrors(EvaluationResults r)
{
    if (r.Errors is not null)
    {
        foreach (var kv in r.Errors)
            yield return (r.InstanceLocation.ToString(), kv.Key, kv.Value);
    }

    if (r.Details is null) yield break;

    foreach (var d in r.Details)
        foreach (var e in EnumerateErrors(d))
            yield return e;
}

// --------------------
// TEST
// --------------------
static async Task<int> RunTests(IEnumerable<string> jsonFiles)
{
    int total = 0, passed = 0, failed = 0, skipped = 0;

    foreach (var file in jsonFiles)
    {
        if (!TryReadJson(file, out var doc, out var err))
        {
            failed++;
            Console.Error.WriteLine(err);
            continue;
        }

        var root = doc!.RootElement;

        if (!root.TryGetProperty("tests", out var testsEl) || testsEl.ValueKind != JsonValueKind.Array)
        {
            Console.WriteLine($"[SKIP] {file} (no tests[])");
            skipped++;
            continue;
        }

        foreach (var t in testsEl.EnumerateArray())
        {
            total++;

            var name = t.TryGetProperty("name", out var n) && n.ValueKind == JsonValueKind.String
                ? n.GetString() ?? "(unnamed)"
                : "(unnamed)";

            var input = t.TryGetProperty("input", out var i) ? i : default;
            var expected = t.TryGetProperty("expected", out var e) ? e : default;

            var (ok, message) = EvaluatePolicyTest(root, input, expected);

            if (ok)
            {
                passed++;
                Console.WriteLine($"[PASS] {Path.GetFileName(file)} :: {name}");
            }
            else
            {
                failed++;
                Console.Error.WriteLine($"[FAIL] {Path.GetFileName(file)} :: {name} — {message}");
            }
        }
    }

    Console.WriteLine($"\nTests complete. Total: {total}, Passed: {passed}, Failed: {failed}, Skipped files: {skipped}");
    return failed == 0 ? 0 : 1;
}

static (bool Ok, string Message) EvaluatePolicyTest(JsonElement packRoot, JsonElement input, JsonElement expected)
{
    if (expected.ValueKind != JsonValueKind.Object)
        return (false, "expected must be an object");

    foreach (var expProp in expected.EnumerateObject())
    {
        var key = expProp.Name;
        var expVal = expProp.Value;

        // 1) Tier 2 checklist gating: allowPromoteTier + blockReason
        if (key.Equals("allowPromoteTier", StringComparison.OrdinalIgnoreCase) ||
            key.Equals("blockReason", StringComparison.OrdinalIgnoreCase))
        {
            var actual = ComputeTier2Gating(packRoot, input);
            if (!TryGetProperty(actual, key, out var actVal))
                return (false, $"Could not compute '{key}'");

            if (!JsonEquals(actVal, expVal))
                return (false, $"Expected {key}={expVal} but got {actVal}");

            continue;
        }

        // 2) Reason taxonomy extraction: ieCodes
        if (key.Equals("ieCodes", StringComparison.OrdinalIgnoreCase))
        {
            var codes = ComputeIrelandReasonCodes(packRoot);
            var actVal = JsonDocument.Parse(JsonSerializer.Serialize(codes)).RootElement;

            if (!JsonEquals(actVal, expVal))
                return (false, $"Expected ieCodes={expVal} but got {actVal}");

            continue;
        }

        // 3) Safeguarding keyword trigger: createAlert + requireHumanReview
        if (key.Equals("createAlert", StringComparison.OrdinalIgnoreCase) ||
            key.Equals("requireHumanReview", StringComparison.OrdinalIgnoreCase))
        {
            var actual = ComputeSafeguarding(packRoot, input);
            if (!TryGetProperty(actual, key, out var actVal))
                return (false, $"Could not compute '{key}'");

            if (!JsonEquals(actVal, expVal))
                return (false, $"Expected {key}={expVal} but got {actVal}");

            continue;
        }


        // 4) Consent gate: allowed / consentAllowed
        if (key.Equals("consentAllowed", StringComparison.OrdinalIgnoreCase) ||
            key.Equals("allowed", StringComparison.OrdinalIgnoreCase))
        {
            var act = ComputeConsentAllowed(packRoot, input);
            var actVal = JsonDocument.Parse(JsonSerializer.Serialize(act)).RootElement;

            if (!JsonEquals(actVal, expVal))
                return (false, $"Expected {key}={expVal} but got {actVal}");

            continue;
        }

        // 5) Consent capture: recorded (platform can record opt-out)
        if (key.Equals("recorded", StringComparison.OrdinalIgnoreCase))
        {
            var act = ComputePlatformCanRecordOptOut(packRoot);
            var actVal = JsonDocument.Parse(JsonSerializer.Serialize(act)).RootElement;

            if (!JsonEquals(actVal, expVal))
                return (false, $"Expected recorded={expVal} but got {actVal}");

            continue;
        }

        // 6) Consent gate: allowed
        if (key.Equals("allowed", StringComparison.OrdinalIgnoreCase) ||
            key.Equals("consentAllowed", StringComparison.OrdinalIgnoreCase))
        {
            var act = ComputeConsentAllowed(packRoot, input);
            var actVal = JsonDocument.Parse(JsonSerializer.Serialize(act)).RootElement;

            if (!JsonEquals(actVal, expVal))
                return (false, $"Expected {key}={expVal} but got {actVal}");

            continue;
        }

        // 7) Consent capture: recorded/newState for OPT_OUT events
        if (key.Equals("recorded", StringComparison.OrdinalIgnoreCase) ||
            key.Equals("newState", StringComparison.OrdinalIgnoreCase))
        {
            var actual = ComputeOptOutEvent(packRoot, input); // returns { recorded, newState }
            if (!TryGetProperty(actual, key, out var actVal))
                return (false, $"Could not compute '{key}'");

            if (!JsonEquals(actVal, expVal))
                return (false, $"Expected {key}={expVal} but got {actVal}");

            continue;
        }

        if (key.Equals("createAlert", StringComparison.OrdinalIgnoreCase) ||
            key.Equals("severity", StringComparison.OrdinalIgnoreCase) ||
            key.Equals("caseType", StringComparison.OrdinalIgnoreCase) ||
            key.Equals("checklistId", StringComparison.OrdinalIgnoreCase))
        {
            var actual = ComputeSafeguarding(packRoot, input);

            if (!TryGetProperty(actual, key, out var actVal))
                return (false, $"Could not compute '{key}'");

            if (!JsonEquals(actVal, expVal))
                return (false, $"Expected {key}={expVal} but got {actVal}");

            continue;
        }



        // Unknown expected key => fail (keeps tests meaningful)
        return (false, $"Unrecognised expected key '{key}'. Add an evaluator for it.");
    }

    return (true, "OK");
}

static Dictionary<string, object?> ComputeOptOutEvent(JsonElement packRoot, JsonElement input)
{
    string evt =
        input.TryGetProperty("event", out var ev) && ev.ValueKind == JsonValueKind.String
            ? (ev.GetString() ?? "")
            : "";

    string source =
        input.TryGetProperty("source", out var so) && so.ValueKind == JsonValueKind.String
            ? (so.GetString() ?? "")
            : "";

    bool canRecord = false;
    bool sourceAllowed = false;

    if (packRoot.TryGetProperty("consentModel", out var cm) &&
        cm.TryGetProperty("consentCapture", out var cap))
    {
        if (cap.TryGetProperty("platformCanRecordOptOut", out var p) &&
            (p.ValueKind == JsonValueKind.True || p.ValueKind == JsonValueKind.False))
            canRecord = p.ValueKind == JsonValueKind.True;

        if (cap.TryGetProperty("sources", out var sources) && sources.ValueKind == JsonValueKind.Array)
        {
            foreach (var s in sources.EnumerateArray())
            {
                if (s.ValueKind == JsonValueKind.String &&
                    source.Equals(s.GetString(), StringComparison.OrdinalIgnoreCase))
                {
                    sourceAllowed = true;
                    break;
                }
            }
        }
    }

    bool recorded = canRecord && sourceAllowed && evt.Equals("OPT_OUT", StringComparison.OrdinalIgnoreCase);

    return new Dictionary<string, object?>
    {
        ["recorded"] = recorded,
        ["newState"] = recorded ? "OPTED_OUT" : "UNCHANGED"
    };
}


static bool ComputePlatformCanRecordOptOut(JsonElement packRoot)
{
    if (packRoot.TryGetProperty("consentModel", out var cm) &&
        cm.TryGetProperty("consentCapture", out var cap) &&
        cap.TryGetProperty("platformCanRecordOptOut", out var p) &&
        (p.ValueKind == JsonValueKind.True || p.ValueKind == JsonValueKind.False))
    {
        return p.ValueKind == JsonValueKind.True;
    }

    return false;
}


static Dictionary<string, object?> ComputeTier2Gating(JsonElement packRoot, JsonElement input)
{
    bool checklistCompleted = input.TryGetProperty("checklistCompleted", out var cc) && cc.ValueKind == JsonValueKind.True;
    string checklistId = "T2_PLAN_DEFAULT";

    if (packRoot.TryGetProperty("attendancePlanPlaybook", out var ap) &&
        ap.TryGetProperty("checklists", out var cls) &&
        cls.ValueKind == JsonValueKind.Array &&
        cls.GetArrayLength() > 0 &&
        cls[0].TryGetProperty("checklistId", out var cid) &&
        cid.ValueKind == JsonValueKind.String)
    {
        checklistId = cid.GetString() ?? checklistId;
    }

    bool allow = checklistCompleted;
    string? blockReason = allow ? null : $"{checklistId} incomplete";

    return new Dictionary<string, object?>
    {
        ["allowPromoteTier"] = allow,
        ["blockReason"] = blockReason
    };
}

static List<string> ComputeIrelandReasonCodes(JsonElement packRoot)
{
    var codes = new List<string>();

    if (packRoot.TryGetProperty("reasonTaxonomy", out var rt) &&
        rt.TryGetProperty("countryDefaults", out var cd) &&
        cd.TryGetProperty("IE", out var ie) &&
        ie.TryGetProperty("codes", out var arr) &&
        arr.ValueKind == JsonValueKind.Array)
    {
        foreach (var c in arr.EnumerateArray())
        {
            if (c.TryGetProperty("code", out var codeEl) && codeEl.ValueKind == JsonValueKind.String)
                codes.Add(codeEl.GetString() ?? "");
        }
    }

    return codes.Where(x => !string.IsNullOrWhiteSpace(x)).ToList();
}

static Dictionary<string, object?> ComputeSafeguarding(JsonElement packRoot, JsonElement input)
{
    bool createAlert = false;
    string? severity = null;
    string caseType = "SAFEGUARDING";
    string? checklistId = null;

    if (!packRoot.TryGetProperty("safeguarding", out var sg))
    {
        return new Dictionary<string, object?>
        {
            ["createAlert"] = false
        };
    }

    if (sg.TryGetProperty("restrictedCaseType", out var rct) && rct.ValueKind == JsonValueKind.String)
        caseType = rct.GetString() ?? caseType;

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
                if (!cond.TryGetProperty("metric", out var metricEl) || metricEl.ValueKind != JsonValueKind.String) { all = false; break; }
                if (!cond.TryGetProperty("op", out var opEl) || opEl.ValueKind != JsonValueKind.String) { all = false; break; }
                if (!cond.TryGetProperty("value", out var valEl)) { all = false; break; }

                var metric = metricEl.GetString()!;
                var op = opEl.GetString()!;

                // Your input uses top-level metrics (e.g. guardianNoReplyDays)
                if (!input.TryGetProperty(metric, out var actualEl))
                {
                    all = false; break;
                }

                if (!Compare(actualEl, op, valEl))
                {
                    all = false; break;
                }
            }

            if (all)
            {
                createAlert = true;

                // Pull severity from action if present
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
        return new Dictionary<string, object?> { ["createAlert"] = false };

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

    return new Dictionary<string, object?>
    {
        ["createAlert"] = true,
        ["severity"] = severity,
        ["caseType"] = caseType,
        ["checklistId"] = checklistId
    };
}

static bool Compare(JsonElement actual, string op, JsonElement expected)
{
    if (TryGetNumber(actual, out var a) && TryGetNumber(expected, out var b))
    {
        return op switch
        {
            ">=" => a >= b,
            ">" => a > b,
            "<=" => a <= b,
            "<" => a < b,
            "==" => a == b,
            "!=" => a != b,
            _ => false
        };
    }

    // String compare (fallback)
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

static bool TryGetNumber(JsonElement el, out double value)
{
    value = 0;
    if (el.ValueKind == JsonValueKind.Number) return el.TryGetDouble(out value);
    if (el.ValueKind == JsonValueKind.String && double.TryParse(el.GetString(), out var d))
    {
        value = d;
        return true;
    }
    return false;
}

static bool ComputeConsentAllowed(JsonElement packRoot, JsonElement input)
{
    string channel =
        input.TryGetProperty("channel", out var ch) && ch.ValueKind == JsonValueKind.String
            ? (ch.GetString() ?? "SMS")
            : "SMS";

    string state =
        input.TryGetProperty("state", out var st) && st.ValueKind == JsonValueKind.String
            ? (st.GetString() ?? "UNKNOWN")
            : "UNKNOWN";

    // Interpret state
    bool optedOut = state.Equals("OPTED_OUT", StringComparison.OrdinalIgnoreCase);
    bool optedIn = state.Equals("OPTED_IN", StringComparison.OrdinalIgnoreCase);

    bool requireExplicitOptIn = false;

    // Read defaultRule.requireExplicitOptIn from consentModel.channels[channel]
    if (packRoot.TryGetProperty("consentModel", out var cm) &&
        cm.TryGetProperty("channels", out var channels) &&
        channels.ValueKind == JsonValueKind.Object &&
        channels.TryGetProperty(channel, out var chan) &&
        chan.TryGetProperty("defaultRule", out var dr) &&
        dr.TryGetProperty("requireExplicitOptIn", out var reo) &&
        (reo.ValueKind == JsonValueKind.True || reo.ValueKind == JsonValueKind.False))
    {
        requireExplicitOptIn = reo.ValueKind == JsonValueKind.True;
    }

    // Safety default: WhatsApp and Voice need explicit opt-in
    if (channel.Equals("WHATSAPP", StringComparison.OrdinalIgnoreCase) ||
        channel.Equals("VOICE_AUTODIAL", StringComparison.OrdinalIgnoreCase) ||
        channel.Equals("VOICE", StringComparison.OrdinalIgnoreCase))
    {
        requireExplicitOptIn = true;
    }

    return !optedOut && (!requireExplicitOptIn || optedIn);
}


static bool TryGetProperty(Dictionary<string, object?> dict, string key, out JsonElement value)
{
    if (!dict.TryGetValue(key, out var v))
    {
        value = default;
        return false;
    }

    var json = JsonSerializer.Serialize(v);
    value = JsonDocument.Parse(json).RootElement;
    return true;
}

static bool TryReadJson(string file, out JsonDocument? doc, out string error)
{
    doc = null;
    error = "";

    try
    {
        var json = File.ReadAllText(file);
        doc = JsonDocument.Parse(json);
        return true;
    }
    catch (Exception ex)
    {
        error = $"[READ/PARSE FAIL] {file}: {ex.Message}";
        return false;
    }
}

static bool JsonEquals(JsonElement a, JsonElement b)
{
    if (a.ValueKind != b.ValueKind)
        return false;

    switch (a.ValueKind)
    {
        case JsonValueKind.Null:
        case JsonValueKind.True:
        case JsonValueKind.False:
            return a.ValueKind == b.ValueKind;

        case JsonValueKind.Number:
            return a.GetDouble().Equals(b.GetDouble());

        case JsonValueKind.String:
            return a.GetString() == b.GetString();

        case JsonValueKind.Array:
            var aa = a.EnumerateArray().ToList();
            var bb = b.EnumerateArray().ToList();
            if (aa.Count != bb.Count) return false;
            for (int i = 0; i < aa.Count; i++)
                if (!JsonEquals(aa[i], bb[i])) return false;
            return true;

        case JsonValueKind.Object:
            var aProps = a.EnumerateObject().ToDictionary(p => p.Name, p => p.Value);
            var bProps = b.EnumerateObject().ToDictionary(p => p.Name, p => p.Value);
            if (aProps.Count != bProps.Count) return false;
            foreach (var (k, av) in aProps)
            {
                if (!bProps.TryGetValue(k, out var bv)) return false;
                if (!JsonEquals(av, bv)) return false;
            }
            return true;

        default:
            return a.ToString() == b.ToString();
    }
}
