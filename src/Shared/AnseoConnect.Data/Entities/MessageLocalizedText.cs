namespace AnseoConnect.Data.Entities;

/// <summary>
/// Localized text for a message in a specific language.
/// </summary>
public sealed class MessageLocalizedText
{
    public Guid LocalizedTextId { get; set; }
    public Guid MessageId { get; set; }
    public string LanguageCode { get; set; } = "en";
    public string Body { get; set; } = string.Empty;
    public bool IsOriginal { get; set; }

    public Message? Message { get; set; }
}
