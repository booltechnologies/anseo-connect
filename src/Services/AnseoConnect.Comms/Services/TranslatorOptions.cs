namespace AnseoConnect.Comms.Services;

public sealed class TranslatorOptions
{
    public string Key { get; set; } = string.Empty;
    public string Region { get; set; } = string.Empty;
    public string Endpoint { get; set; } = "https://api.cognitive.microsofttranslator.com";
    public int CacheHours { get; set; } = 12;
}
