using AnseoConnect.Client.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AnseoConnect.Client;

public sealed class StudentsClient : ApiClientBase
{
    public StudentsClient(HttpClient httpClient, IOptions<ApiClientOptions> options, SampleDataProvider sampleData, ILogger<StudentsClient> logger)
        : base(httpClient, options, sampleData, logger)
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
        if (result != null)
        {
            return result;
        }

        var filtered = SampleData.Students.AsEnumerable();
        if (!string.IsNullOrWhiteSpace(query))
        {
            filtered = filtered.Where(s => s.Name.Contains(query, StringComparison.OrdinalIgnoreCase));
        }
        if (!string.IsNullOrWhiteSpace(year))
        {
            filtered = filtered.Where(s => s.YearGroup.Contains(year, StringComparison.OrdinalIgnoreCase));
        }
        if (!string.IsNullOrWhiteSpace(risk))
        {
            filtered = filtered.Where(s => string.Equals(s.Risk, risk, StringComparison.OrdinalIgnoreCase));
        }

        var items = filtered.Skip(skip).Take(take).ToList();
        var totalCount = filtered.Count();
        return new PagedResult<StudentSummary>(items, totalCount, skip, take, (skip + take) < totalCount);
    }

    public async Task<StudentProfile?> GetStudentAsync(Guid studentId, CancellationToken cancellationToken = default)
    {
        var result = await GetOrDefaultAsync<StudentProfile>($"api/students/{studentId}", cancellationToken);
        return result ?? SampleData.FindStudent(studentId);
    }
}
