using AnseoConnect.Data.MultiTenancy;

namespace AnseoConnect.Data.Entities;

/// <summary>
/// Cache entry for translated text to reduce external API calls.
/// </summary>
public sealed class TranslationCache : ITenantScoped
{
    public Guid TranslationCacheId { get; set; }
    public Guid TenantId { get; set; }
    public string Hash { get; set; } = string.Empty;
    public string FromLanguage { get; set; } = "en";
    public string ToLanguage { get; set; } = "en";
    public string TranslatedText { get; set; } = string.Empty;
    public DateTimeOffset CachedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? ExpiresAtUtc { get; set; }
}
