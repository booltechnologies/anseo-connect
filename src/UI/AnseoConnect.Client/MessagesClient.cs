using AnseoConnect.Client.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AnseoConnect.Client;

public sealed class MessagesClient : ApiClientBase
{
    public MessagesClient(HttpClient httpClient, IOptions<ApiClientOptions> options, SampleDataProvider sampleData, ILogger<MessagesClient> logger)
        : base(httpClient, options, sampleData, logger)
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
        if (result != null)
        {
            return result;
        }

        var all = SampleData.Messages.AsEnumerable();
        if (!string.IsNullOrWhiteSpace(channel)) all = all.Where(m => string.Equals(m.Channel, channel, StringComparison.OrdinalIgnoreCase));
        if (!string.IsNullOrWhiteSpace(status)) all = all.Where(m => string.Equals(m.Status, status, StringComparison.OrdinalIgnoreCase));
        if (!string.IsNullOrWhiteSpace(messageType)) all = all.Where(m => string.Equals(m.MessageType, messageType, StringComparison.OrdinalIgnoreCase));
        if (failedOnly.HasValue && failedOnly.Value) all = all.Where(m => string.Equals(m.Status, "Failed", StringComparison.OrdinalIgnoreCase));
        if (studentId.HasValue) all = all.Where(m => m.StudentId == studentId.Value);
        if (start.HasValue) all = all.Where(m => m.CreatedAtUtc >= start.Value);
        if (end.HasValue) all = all.Where(m => m.CreatedAtUtc <= end.Value);

        var list = all.ToList();
        var items = list.Skip(skip).Take(take).ToList();
        var totalCount = list.Count;
        return new PagedResult<MessageSummary>(items, totalCount, skip, take, (skip + take) < totalCount);
    }

    public async Task<MessageDetail?> GetMessageAsync(Guid messageId, CancellationToken cancellationToken = default)
    {
        var result = await GetOrDefaultAsync<MessageDetail>($"api/messages/{messageId}", cancellationToken);
        return result ?? SampleData.FindMessage(messageId);
    }

    public async Task<bool> SendMessageAsync(MessageComposeRequest request, CancellationToken cancellationToken = default)
    {
        var response = await PostOrDefaultAsync<MessageComposeRequest, object>("api/messages/send", request, cancellationToken);
        if (response != null)
        {
            return true;
        }

        // Stubbed add when API is unavailable
        SampleData.AddMessage(request);
        return true;
    }
}
