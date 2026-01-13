using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using AnseoConnect.Data;
using AnseoConnect.Data.Entities;
using AnseoConnect.Data.MultiTenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace AnseoConnect.Comms.Services;

/// <summary>
/// Placeholder Azure Translator-backed implementation with database caching.
/// Replace stubbed translation call with real Azure Cognitive Services translator.
/// </summary>
public sealed class AzureTranslatorService : ITranslationService
{
    private readonly AnseoConnectDbContext _dbContext;
    private readonly ILogger<AzureTranslatorService> _logger;
    private readonly ITenantContext _tenantContext;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly TranslatorOptions _options;

    public AzureTranslatorService(
        AnseoConnectDbContext dbContext,
        ITenantContext tenantContext,
        IHttpClientFactory httpClientFactory,
        IOptions<TranslatorOptions> options,
        ILogger<AzureTranslatorService> logger)
    {
        _dbContext = dbContext;
        _tenantContext = tenantContext;
        _httpClientFactory = httpClientFactory;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<string> TranslateAsync(string text, string fromLanguage, string toLanguage, CancellationToken ct)
    {
        if (string.Equals(fromLanguage, toLanguage, StringComparison.OrdinalIgnoreCase))
        {
            return text;
        }

        var hash = ComputeHash(text, fromLanguage, toLanguage);
        var cached = await _dbContext.TranslationCaches
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Hash == hash, ct);
        if (cached != null && (!cached.ExpiresAtUtc.HasValue || cached.ExpiresAtUtc > DateTimeOffset.UtcNow))
        {
            return cached.TranslatedText;
        }

        var translated = await TranslateViaAzureAsync(text, fromLanguage, toLanguage, ct);

        var cache = new TranslationCache
        {
            TranslationCacheId = Guid.NewGuid(),
            TenantId = _tenantContext.TenantId,
            Hash = hash,
            FromLanguage = fromLanguage,
            ToLanguage = toLanguage,
            TranslatedText = translated,
            CachedAtUtc = DateTimeOffset.UtcNow,
            ExpiresAtUtc = DateTimeOffset.UtcNow.AddHours(Math.Max(1, _options.CacheHours))
        };

        _dbContext.TranslationCaches.Add(cache);
        await _dbContext.SaveChangesAsync(ct);

        _logger.LogInformation("Cached translation for hash {Hash}", hash);
        return translated;
    }

    private static string ComputeHash(string text, string from, string to)
    {
        using var sha = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes($"{from}->{to}:{text}");
        var hashBytes = sha.ComputeHash(bytes);
        return Convert.ToHexString(hashBytes);
    }

    private async Task<string> TranslateViaAzureAsync(string text, string fromLanguage, string toLanguage, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(_options.Key) || string.IsNullOrWhiteSpace(_options.Region))
        {
            throw new InvalidOperationException("Azure Translator key/region not configured.");
        }

        var endpoint = string.IsNullOrWhiteSpace(_options.Endpoint)
            ? "https://api.cognitive.microsofttranslator.com"
            : _options.Endpoint;

        var requestUri = $"{endpoint.TrimEnd('/')}/translate?api-version=3.0&from={fromLanguage}&to={toLanguage}";
        var client = _httpClientFactory.CreateClient("AzureTranslator");

        using var request = new HttpRequestMessage(HttpMethod.Post, requestUri);
        request.Headers.Add("Ocp-Apim-Subscription-Key", _options.Key);
        request.Headers.Add("Ocp-Apim-Subscription-Region", _options.Region);
        request.Content = JsonContent.Create(new[] { new { Text = text } });

        var response = await client.SendAsync(request, ct);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            _logger.LogError("Translator API error {Status}: {Body}", response.StatusCode, body);
            throw new InvalidOperationException($"Translator API error: {response.StatusCode}");
        }

        var json = await response.Content.ReadAsStringAsync(ct);
        using var doc = JsonDocument.Parse(json);
        var translations = doc.RootElement[0].GetProperty("translations");
        if (translations.GetArrayLength() == 0)
        {
            throw new InvalidOperationException("Translator returned no translations.");
        }

        return translations[0].GetProperty("text").GetString() ?? text;
    }
}
