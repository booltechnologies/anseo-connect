using System.Text.Json;

namespace AnseoConnect.PolicyRuntime;

/// <summary>
/// Interface for evaluating consent rules from policy packs.
/// </summary>
public interface IConsentEvaluator
{
    /// <summary>
    /// Evaluates whether consent is allowed for a given channel and state.
    /// </summary>
    /// <param name="policyPackRoot">The root JSON element of the policy pack</param>
    /// <param name="channel">The communication channel (SMS, EMAIL, WHATSAPP, VOICE_AUTODIAL)</param>
    /// <param name="currentState">The current consent state (UNKNOWN, OPTED_IN, OPTED_OUT)</param>
    /// <returns>True if consent is allowed, false otherwise</returns>
    bool EvaluateConsent(JsonElement policyPackRoot, string channel, string currentState);
}
