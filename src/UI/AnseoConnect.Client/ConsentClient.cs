using AnseoConnect.Client.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AnseoConnect.Client;

public sealed class ConsentClient : ApiClientBase
{
    public ConsentClient(HttpClient httpClient, IOptions<ApiClientOptions> options, SampleDataProvider sampleData, ILogger<ConsentClient> logger)
        : base(httpClient, options, sampleData, logger)
    {
    }

    public async Task<ConsentStatusDto?> GetConsentStatusAsync(Guid guardianId, string channel, CancellationToken cancellationToken = default)
    {
        if (guardianId == Guid.Empty || string.IsNullOrWhiteSpace(channel))
        {
            return null;
        }

        var result = await GetOrDefaultAsync<ConsentStatusDto>($"api/consent/{guardianId}?channel={channel}", cancellationToken);
        if (result != null)
        {
            return result;
        }

        // Fallback stub
        return new ConsentStatusDto(guardianId, "Guardian", channel, "UNKNOWN", DateTimeOffset.UtcNow, "SYSTEM");
    }
}
