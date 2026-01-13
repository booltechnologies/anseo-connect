using AnseoConnect.Client.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AnseoConnect.Client;

public sealed class StudentsClient : ApiClientBase
{
    public StudentsClient(HttpClient httpClient, IOptions<ApiClientOptions> options, ILogger<StudentsClient> logger)
        : base(httpClient, options, logger)
    {
    }

    public async Task<PagedResult<StudentSummary>> SearchAsync(
        string? query = null,
        string? year = null,
        string? externalId = null,
        string? risk = null,
        int skip = 0,
        int take = 50,
        CancellationToken cancellationToken = default)
    {
        var endpoint = $"api/students?query={Uri.EscapeDataString(query ?? string.Empty)}&skip={skip}&take={take}";
        if (!string.IsNullOrWhiteSpace(year)) endpoint += $"&year={Uri.EscapeDataString(year)}";
        if (!string.IsNullOrWhiteSpace(externalId)) endpoint += $"&externalId={Uri.EscapeDataString(externalId)}";
        if (!string.IsNullOrWhiteSpace(risk)) endpoint += $"&risk={Uri.EscapeDataString(risk)}";

        var result = await GetOrDefaultAsync<PagedResult<StudentSummary>>(endpoint, cancellationToken);
        return result ?? new PagedResult<StudentSummary>(Array.Empty<StudentSummary>(), 0, skip, take, false);
    }

    public async Task<StudentProfile?> GetStudentAsync(Guid studentId, CancellationToken cancellationToken = default)
    {
        var result = await GetOrDefaultAsync<StudentProfile>($"api/students/{studentId}", cancellationToken);
        return result;
    }
}
