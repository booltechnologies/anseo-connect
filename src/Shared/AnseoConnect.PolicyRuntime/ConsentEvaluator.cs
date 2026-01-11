using System.Text.Json;

namespace AnseoConnect.PolicyRuntime;

/// <summary>
/// Implementation of IConsentEvaluator using policy pack logic.
/// Reuses the same logic as PolicyPackTool.ComputeConsentAllowed.
/// </summary>
public sealed class ConsentEvaluator : IConsentEvaluator
{
    public bool EvaluateConsent(JsonElement policyPackRoot, string channel, string currentState)
    {
        // Normalize channel
        channel = channel?.ToUpperInvariant() ?? "SMS";
        currentState = currentState?.ToUpperInvariant() ?? "UNKNOWN";

        // Interpret state
        bool optedOut = currentState.Equals("OPTED_OUT", StringComparison.OrdinalIgnoreCase);
        bool optedIn = currentState.Equals("OPTED_IN", StringComparison.OrdinalIgnoreCase);

        bool requireExplicitOptIn = false;

        // Read defaultRule.requireExplicitOptIn from consentModel.channels[channel]
        if (policyPackRoot.TryGetProperty("consentModel", out var cm) &&
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

        // Consent is allowed if:
        // - Not opted out AND
        // - (Either explicit opt-in not required OR opted in)
        return !optedOut && (!requireExplicitOptIn || optedIn);
    }
}
