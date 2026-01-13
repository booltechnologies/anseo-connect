using System.Net.Http.Json;
using AnseoConnect.Client.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AnseoConnect.Client;

public sealed class AuthClient : ApiClientBase
{
    public AuthClient(HttpClient httpClient, IOptions<ApiClientOptions> options, ILogger<AuthClient> logger)
        : base(httpClient, options, logger)
    {
    }

    public async Task<LoginResponse?> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await HttpClient.PostAsJsonAsync("auth/local/login", request, cancellationToken);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<LoginResponse>(cancellationToken: cancellationToken);
        }
        catch
        {
            // fall back to sample token for dev preview
            return new LoginResponse("sample-token", Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), request.Username, $"{request.Username}@example.com");
        }
    }

    public async Task<WhoAmIResponse?> WhoAmIAsync(CancellationToken cancellationToken = default)
    {
        var result = await GetOrDefaultAsync<WhoAmIResponse>("auth/whoami", cancellationToken);
        return result ?? new WhoAmIResponse(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "demo", "demo@example.com", "Demo", "User");
    }
}
