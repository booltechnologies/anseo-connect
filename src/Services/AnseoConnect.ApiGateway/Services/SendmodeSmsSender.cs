using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace AnseoConnect.ApiGateway.Services;

public sealed class SendmodeSmsSender : ISmsSender
{
    private readonly HttpClient _httpClient;
    private readonly string _username;
    private readonly string _password;
    private readonly string _apiUrl;
    private readonly string? _from;
    private readonly ILogger<SendmodeSmsSender> _logger;

    public SendmodeSmsSender(HttpClient httpClient, string username, string password, string? from, string apiUrl, ILogger<SendmodeSmsSender> logger)
    {
        _httpClient = httpClient;
        _username = username;
        _password = password;
        _from = from;
        _apiUrl = string.IsNullOrWhiteSpace(apiUrl) ? "https://api.sendmode.com/v2/sendSMS" : apiUrl;
        _logger = logger;

        var authValue = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_username}:{_password}"));
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", authValue);
    }

    public async Task SendAsync(string toE164, string message, CancellationToken ct)
    {
        var requestData = new Dictionary<string, string>
        {
            { "to", toE164 },
            { "message", message }
        };
        if (!string.IsNullOrWhiteSpace(_from))
        {
            requestData["from"] = _from!;
        }

        var content = new FormUrlEncodedContent(requestData);
        var response = await _httpClient.PostAsync(_apiUrl, content, ct);
        var body = await response.Content.ReadAsStringAsync(ct);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Sendmode SMS failed {Status}: {Body}", response.StatusCode, body);
            throw new InvalidOperationException($"Sendmode SMS error: {response.StatusCode}");
        }

        try
        {
            var doc = JsonDocument.Parse(body);
            var status = doc.RootElement.TryGetProperty("status", out var statusEl) ? statusEl.GetString() : "sent";
            var id = doc.RootElement.TryGetProperty("messageId", out var idEl) ? idEl.GetString() : null;
            _logger.LogInformation("Sendmode SMS sent id={Id} status={Status}", id, status);
        }
        catch (JsonException)
        {
            _logger.LogInformation("Sendmode SMS sent. Response: {Body}", body);
        }
    }
}
