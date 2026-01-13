namespace AnseoConnect.Comms.Services;

/// <summary>
/// Standardized result from outbound provider sends.
/// </summary>
public sealed record SendResult(
    bool Success,
    string? ProviderMessageId = null,
    string? Status = null,
    string? ErrorCode = null,
    string? ErrorMessage = null);
