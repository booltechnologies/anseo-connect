namespace AnseoConnect.Comms.Services;

public interface ITranslationService
{
    Task<string> TranslateAsync(string text, string fromLanguage, string toLanguage, CancellationToken ct);
}
