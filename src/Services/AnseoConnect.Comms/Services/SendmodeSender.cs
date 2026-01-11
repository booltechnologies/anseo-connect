using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace AnseoConnect.Comms.Services;

/// <summary>
/// Service for sending SMS messages via Sendmode REST API.
/// Based on Sendmode REST API documentation: https://developers.sendmode.com/restdocs
/// </summary>
public sealed class SendmodeSender
{
    private readonly HttpClient _httpClient;
    private readonly string _username;
    private readonly string _password;
    private readonly string? _fromNumber;
    private readonly string _apiUrl;
    private readonly ILogger<SendmodeSender> _logger;

    public SendmodeSender(
        HttpClient httpClient,
        string username,
        string password,
        string? fromNumber,
        ILogger<SendmodeSender> logger,
        string? apiUrl = null)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _username = username ?? throw new ArgumentNullException(nameof(username));
        _password = password ?? throw new ArgumentNullException(nameof(password));
        _fromNumber = fromNumber;
        _apiUrl = apiUrl ?? "https://api.sendmode.com/v2/sendSMS";
        _logger = logger;

        // Set basic authentication header if credentials provided
        // Note: If using API key, username and password will both be the API key
        if (!string.IsNullOrWhiteSpace(_username) && !string.IsNullOrWhiteSpace(_password))
        {
            var authValue = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_username}:{_password}"));
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", authValue);
        }
    }

    /// <summary>
    /// Sends an SMS message via Sendmode API.
    /// </summary>
    public async Task<SendmodeSendResult> SendSmsAsync(string to, string body, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Sending SMS to {To} via Sendmode", to);

            // Sendmode API typically uses form-encoded POST request
            // Format may vary based on Sendmode API version - adjust endpoint as needed
            var requestData = new Dictionary<string, string>
            {
                { "to", to },
                { "message", body }
            };

            // Add from number if configured (may be optional if account has default)
            if (!string.IsNullOrWhiteSpace(_fromNumber))
            {
                requestData["from"] = _fromNumber;
            }

            // Use configured API URL or default endpoint
            var content = new FormUrlEncodedContent(requestData);
            var response = await _httpClient.PostAsync(_apiUrl, content, cancellationToken);

            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError(
                    "Failed to send SMS via Sendmode. Status: {Status}, Response: {Response}",
                    response.StatusCode,
                    responseBody);
                return new SendmodeSendResult
                {
                    Success = false,
                    ErrorMessage = $"Sendmode API error: {response.StatusCode} - {responseBody}"
                };
            }

            // Parse response - Sendmode response format may vary
            // Common formats: JSON with message ID, or plain text
            // Example: {"messageId": "12345", "status": "sent"} or simple text response
            string? messageId = null;
            string? status = null;

            try
            {
                var jsonDoc = JsonDocument.Parse(responseBody);
                if (jsonDoc.RootElement.TryGetProperty("messageId", out var msgId))
                {
                    messageId = msgId.GetString();
                }
                else if (jsonDoc.RootElement.TryGetProperty("id", out var id))
                {
                    messageId = id.GetString();
                }
                if (jsonDoc.RootElement.TryGetProperty("status", out var stat))
                {
                    status = stat.GetString();
                }
            }
            catch (JsonException)
            {
                // Response might not be JSON - treat entire response as message ID if simple text
                if (!string.IsNullOrWhiteSpace(responseBody))
                {
                    messageId = responseBody.Trim();
                    status = "sent";
                }
            }

            _logger.LogInformation(
                "SMS sent successfully via Sendmode. MessageId: {MessageId}, Status: {Status}",
                messageId,
                status ?? "unknown");

            return new SendmodeSendResult
            {
                Success = true,
                ProviderMessageId = messageId,
                Status = status ?? "sent"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send SMS to {To} via Sendmode", to);
            return new SendmodeSendResult
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }
}

public sealed record SendmodeSendResult
{
    public bool Success { get; set; }
    public string? ProviderMessageId { get; set; }
    public string? Status { get; set; }
    public string? ErrorMessage { get; set; }
}
