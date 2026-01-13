using AnseoConnect.Client.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AnseoConnect.Client;

public sealed class MessagesClient : ApiClientBase
{
    public MessagesClient(HttpClient httpClient, IOptions<ApiClientOptions> options, ILogger<MessagesClient> logger)
        : base(httpClient, options, logger)
    {
    }

    public async Task<PagedResult<MessageSummary>> GetMessagesAsync(
        string? channel = null,
        string? status = null,
        string? messageType = null,
        DateTimeOffset? start = null,
        DateTimeOffset? end = null,
        bool? failedOnly = null,
        Guid? studentId = null,
        int skip = 0,
        int take = 50,
        CancellationToken cancellationToken = default)
    {
        var query = $"api/messages?skip={skip}&take={take}";
        if (!string.IsNullOrWhiteSpace(channel)) query += $"&channel={channel}";
        if (!string.IsNullOrWhiteSpace(status)) query += $"&status={status}";
        if (!string.IsNullOrWhiteSpace(messageType)) query += $"&messageType={messageType}";
        if (start.HasValue) query += $"&start={start.Value:O}";
        if (end.HasValue) query += $"&end={end.Value:O}";
        if (failedOnly.HasValue && failedOnly.Value) query += $"&failedOnly=true";
        if (studentId.HasValue) query += $"&studentId={studentId.Value}";

        var result = await GetOrDefaultAsync<PagedResult<MessageSummary>>(query, cancellationToken);
        return result ?? new PagedResult<MessageSummary>(Array.Empty<MessageSummary>(), 0, skip, take, false);
    }

    public async Task<MessageDetail?> GetMessageAsync(Guid messageId, CancellationToken cancellationToken = default)
    {
        var result = await GetOrDefaultAsync<MessageDetail>($"api/messages/{messageId}", cancellationToken);
        return result;
    }

    public async Task<bool> SendMessageAsync(MessageComposeRequest request, CancellationToken cancellationToken = default)
    {
        var response = await PostOrDefaultAsync<MessageComposeRequest, object>("api/messages/send", request, cancellationToken);
        return response != null;
    }
}
