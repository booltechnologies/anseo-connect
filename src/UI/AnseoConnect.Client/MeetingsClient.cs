using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using AnseoConnect.Client.Models;

namespace AnseoConnect.Client;

public sealed class MeetingsClient : ApiClientBase
{
    public MeetingsClient(HttpClient httpClient, IOptions<ApiClientOptions> options, ILogger<MeetingsClient> logger)
        : base(httpClient, options, logger)
    {
    }

    public Task<List<InterventionMeetingDto>?> GetMeetingsAsync(CancellationToken cancellationToken = default)
        => GetOrDefaultAsync<List<InterventionMeetingDto>>("api/meetings", cancellationToken);

    public Task<bool> RecordOutcomeAsync(Guid meetingId, MeetingOutcomeRequest request, CancellationToken cancellationToken = default)
        => PutOrFalseAsync($"api/meetings/{meetingId}", request, cancellationToken);
}

