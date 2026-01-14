using AnseoConnect.Client.Models;
using AnseoConnect.Data.Entities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net.Http.Json;

namespace AnseoConnect.Client;

public sealed class AlertsClient : ApiClientBase
{
    public AlertsClient(HttpClient httpClient, IOptions<ApiClientOptions> options, ILogger<AlertsClient> logger)
        : base(httpClient, options, logger)
    {
    }

    public async Task<List<AlertRule>> GetAlertRulesAsync(CancellationToken cancellationToken = default)
    {
        var result = await GetOrDefaultAsync<List<AlertRule>>("api/admin/alerts/rules", cancellationToken);
        return result ?? new List<AlertRule>();
    }

    public async Task<AlertRule?> GetAlertRuleAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await GetOrDefaultAsync<AlertRule>($"api/admin/alerts/rules/{id}", cancellationToken);
    }

    public async Task<AlertRule?> CreateAlertRuleAsync(AlertRule rule, CancellationToken cancellationToken = default)
    {
        return await PostOrDefaultAsync<AlertRule, AlertRule>("api/admin/alerts/rules", rule, cancellationToken);
    }

    public async Task<PagedResult<AlertInstance>> GetAlertInstancesAsync(
        string? status = "Active",
        int skip = 0,
        int take = 50,
        CancellationToken cancellationToken = default)
    {
        var endpoint = $"api/admin/alerts/instances?status={Uri.EscapeDataString(status ?? "")}&skip={skip}&take={take}";

        var result = await GetOrDefaultAsync<PagedResult<AlertInstance>>(endpoint, cancellationToken);
        return result ?? new PagedResult<AlertInstance>(new List<AlertInstance>(), 0, skip, take, false);
    }

    public async Task<AlertInstance?> AcknowledgeAlertAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            var request = new HttpRequestMessage(HttpMethod.Patch, $"api/admin/alerts/instances/{id}/acknowledge");
            var response = await HttpClient.SendAsync(request, cancellationToken);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<AlertInstance>(cancellationToken: cancellationToken);
        }
        catch
        {
            return null;
        }
    }

    public async Task<AlertInstance?> ResolveAlertAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            var request = new HttpRequestMessage(HttpMethod.Patch, $"api/admin/alerts/instances/{id}/resolve");
            var response = await HttpClient.SendAsync(request, cancellationToken);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<AlertInstance>(cancellationToken: cancellationToken);
        }
        catch
        {
            return null;
        }
    }
}
