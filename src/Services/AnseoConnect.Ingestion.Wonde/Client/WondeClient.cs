using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AnseoConnect.Ingestion.Wonde.Client;

/// <summary>
/// Implementation of IWondeClient using Wonde API.
/// </summary>
public sealed class WondeClient : IWondeClient, IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<WondeClient> _logger;
    private readonly string _defaultDomain;
    private readonly bool _disposeHttpClient;

    public WondeClient(HttpClient httpClient, string apiToken, string? defaultDomain = null, ILogger<WondeClient> logger = null!, bool disposeHttpClient = false)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _defaultDomain = defaultDomain ?? "api.wonde.com";
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _disposeHttpClient = disposeHttpClient;
        
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiToken);
        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        _httpClient.Timeout = TimeSpan.FromMinutes(5);
    }

    public async Task<WondeSchoolResponse?> GetSchoolAsync(string schoolId, CancellationToken cancellationToken = default)
    {
        var url = BuildUrl(_defaultDomain, $"schools/{schoolId}");
        return await GetAsync<WondeSchoolResponse>(url, cancellationToken);
    }

    public async Task<WondePagedResponse<WondeStudent>> GetStudentsAsync(
        string schoolId,
        DateTimeOffset? updatedAfter = null,
        CancellationToken cancellationToken = default)
    {
        var queryParams = new List<string>();
        if (updatedAfter.HasValue)
        {
            queryParams.Add($"updated_after={updatedAfter.Value:yyyy-MM-dd HH:mm:ss}");
        }
        queryParams.Add("cursor=true"); // Use cursor pagination for efficiency
        queryParams.Add("include=contacts,contacts.contact_details");

        var url = BuildUrl(_defaultDomain, $"schools/{schoolId}/students", queryParams);
        return await GetAllPagesAsync<WondeStudent>(url, cancellationToken);
    }

    public async Task<WondePagedResponse<WondeContact>> GetContactsAsync(
        string schoolId,
        DateTimeOffset? updatedAfter = null,
        CancellationToken cancellationToken = default)
    {
        var queryParams = new List<string>();
        if (updatedAfter.HasValue)
        {
            queryParams.Add($"updated_after={updatedAfter.Value:yyyy-MM-dd HH:mm:ss}");
        }
        queryParams.Add("cursor=true");
        queryParams.Add("include=contact_details");

        var url = BuildUrl(_defaultDomain, $"schools/{schoolId}/contacts", queryParams);
        return await GetAllPagesAsync<WondeContact>(url, cancellationToken);
    }

    public async Task<WondePagedResponse<WondeAttendance>> GetAttendanceAsync(
        string schoolId,
        DateOnly date,
        CancellationToken cancellationToken = default)
    {
        var queryParams = new List<string>
        {
            $"date={date:yyyy-MM-dd}",
            "cursor=true"
        };

        var url = BuildUrl(_defaultDomain, $"schools/{schoolId}/attendance", queryParams);
        return await GetAllPagesAsync<WondeAttendance>(url, cancellationToken);
    }

    public async Task<WondePagedResponse<WondeStudentAbsence>> GetStudentAbsencesAsync(
        string schoolId,
        DateOnly? fromDate = null,
        DateOnly? toDate = null,
        CancellationToken cancellationToken = default)
    {
        var queryParams = new List<string> { "cursor=true" };
        
        if (fromDate.HasValue)
        {
            queryParams.Add($"from={fromDate.Value:yyyy-MM-dd}");
        }
        if (toDate.HasValue)
        {
            queryParams.Add($"to={toDate.Value:yyyy-MM-dd}");
        }

        var url = BuildUrl(_defaultDomain, $"schools/{schoolId}/attendance/absence", queryParams);
        return await GetAllPagesAsync<WondeStudentAbsence>(url, cancellationToken);
    }

    private string BuildUrl(string domain, string resource, List<string>? queryParams = null)
    {
        var baseUrl = $"https://{domain}/v1.0/{resource}";
        if (queryParams != null && queryParams.Count > 0)
        {
            baseUrl += "?" + string.Join("&", queryParams);
        }
        return baseUrl;
    }

    private async Task<T?> GetAsync<T>(string url, CancellationToken cancellationToken) where T : class
    {
        var maxRetries = 3;
        var delay = TimeSpan.FromSeconds(1);

        for (int attempt = 0; attempt < maxRetries; attempt++)
        {
            try
            {
                _logger.LogDebug("GET {Url} (attempt {Attempt})", url, attempt + 1);
                var response = await _httpClient.GetAsync(url, cancellationToken);

                if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
                {
                    var retryAfter = response.Headers.RetryAfter?.Delta ?? delay;
                    _logger.LogWarning("Rate limited. Waiting {Delay} before retry", retryAfter);
                    await Task.Delay(retryAfter, cancellationToken);
                    continue;
                }

                response.EnsureSuccessStatusCode();

                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
                };

                var result = await response.Content.ReadFromJsonAsync<T>(options, cancellationToken);
                return result;
            }
            catch (HttpRequestException ex) when (attempt < maxRetries - 1)
            {
                _logger.LogWarning(ex, "Request failed (attempt {Attempt}), retrying...", attempt + 1);
                await Task.Delay(delay * (attempt + 1), cancellationToken); // Exponential backoff
            }
        }

        throw new InvalidOperationException($"Failed to get {url} after {maxRetries} attempts");
    }

    private async Task<WondePagedResponse<T>> GetAllPagesAsync<T>(string firstPageUrl, CancellationToken cancellationToken)
    {
        var allData = new List<T>();
        var currentUrl = firstPageUrl;
        var totalRequests = 0;
        const int maxRequests = 1000; // Safety limit
        WondeMeta? lastMeta = null;

        while (!string.IsNullOrEmpty(currentUrl) && totalRequests < maxRequests)
        {
            totalRequests++;
            var response = await GetAsync<WondePagedResponse<T>>(currentUrl, cancellationToken);
            
            if (response == null || response.Data == null)
            {
                break;
            }

            allData.AddRange(response.Data);
            lastMeta = response.Meta;

            // Check if there's a next page (cursor pagination)
            if (response.Meta?.Pagination != null && response.Meta.Pagination.More && !string.IsNullOrEmpty(response.Meta.Pagination.Next))
            {
                currentUrl = response.Meta.Pagination.Next;
            }
            else
            {
                break;
            }
        }

        return new WondePagedResponse<T>(allData, lastMeta);
    }

    public void Dispose()
    {
        if (_disposeHttpClient)
        {
            _httpClient?.Dispose();
        }
    }
}
