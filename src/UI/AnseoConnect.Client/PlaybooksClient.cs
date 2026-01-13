using AnseoConnect.Client.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AnseoConnect.Client;

public sealed class PlaybooksClient : ApiClientBase
{
    public PlaybooksClient(HttpClient httpClient, IOptions<ApiClientOptions> options, ILogger<PlaybooksClient> logger)
        : base(httpClient, options, logger)
    {
    }

    public Task<IReadOnlyList<PlaybookDefinitionDto>?> GetDefinitionsAsync(CancellationToken cancellationToken = default) =>
        GetOrDefaultAsync<IReadOnlyList<PlaybookDefinitionDto>>("api/playbooks", cancellationToken);

    public Task<PlaybookDetailDto?> GetPlaybookAsync(Guid playbookId, CancellationToken cancellationToken = default) =>
        GetOrDefaultAsync<PlaybookDetailDto>($"api/playbooks/{playbookId}", cancellationToken);

    public Task<PlaybookDefinitionDto?> CreateAsync(PlaybookDefinitionDto request, CancellationToken cancellationToken = default) =>
        PostOrDefaultAsync<PlaybookDefinitionDto, PlaybookDefinitionDto>("api/playbooks", request, cancellationToken);

    public Task<PlaybookDefinitionDto?> UpdateAsync(Guid playbookId, PlaybookDefinitionDto request, CancellationToken cancellationToken = default) =>
        PutOrFalseAsync($"api/playbooks/{playbookId}", request, cancellationToken)
            .ContinueWith(t => t.Result ? request : null as PlaybookDefinitionDto, cancellationToken);

    public Task<bool> DeleteAsync(Guid playbookId, CancellationToken cancellationToken = default) =>
        DeleteOrFalseAsync($"api/playbooks/{playbookId}", cancellationToken);

    public Task<PlaybookStepDto?> AddStepAsync(Guid playbookId, PlaybookStepDto request, CancellationToken cancellationToken = default) =>
        PostOrDefaultAsync<PlaybookStepDto, PlaybookStepDto>($"api/playbooks/{playbookId}/steps", request, cancellationToken);

    public Task<PlaybookStepDto?> UpdateStepAsync(Guid playbookId, Guid stepId, PlaybookStepDto request, CancellationToken cancellationToken = default) =>
        PutOrFalseAsync($"api/playbooks/{playbookId}/steps/{stepId}", request, cancellationToken)
            .ContinueWith(t => t.Result ? request : null as PlaybookStepDto, cancellationToken);

    public Task<bool> DeleteStepAsync(Guid playbookId, Guid stepId, CancellationToken cancellationToken = default) =>
        DeleteOrFalseAsync($"api/playbooks/{playbookId}/steps/{stepId}", cancellationToken);

    public Task<IReadOnlyList<PlaybookRunDto>?> GetRunsAsync(string? status = null, CancellationToken cancellationToken = default)
    {
        var path = string.IsNullOrWhiteSpace(status) ? "api/playbooks/runs" : $"api/playbooks/runs?status={status}";
        return GetOrDefaultAsync<IReadOnlyList<PlaybookRunDto>>(path, cancellationToken);
    }

    public Task<PlaybookRunDetailDto?> GetRunAsync(Guid runId, CancellationToken cancellationToken = default) =>
        GetOrDefaultAsync<PlaybookRunDetailDto>($"api/playbooks/runs/{runId}", cancellationToken);

    public Task<bool> StopRunAsync(Guid runId, CancellationToken cancellationToken = default) =>
        PostOrDefaultAsync<object, object>($"api/playbooks/runs/{runId}/stop", new { }, cancellationToken)
            .ContinueWith(t => t.Result != null, cancellationToken);
}
