using System.Net.Http.Headers;
using Microsoft.Extensions.Logging;

namespace AnseoConnect.Client;

public interface IClientTokenProvider
{
    ValueTask<string?> GetTokenAsync(CancellationToken cancellationToken = default);
}

public sealed class BearerTokenHandler : DelegatingHandler
{
    private readonly IClientTokenProvider _tokenProvider;
    private readonly ILogger<BearerTokenHandler> _logger;

    public BearerTokenHandler(IClientTokenProvider tokenProvider, ILogger<BearerTokenHandler> logger)
    {
        _tokenProvider = tokenProvider;
        _logger = logger;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var token = await _tokenProvider.GetTokenAsync(cancellationToken);
        if (!string.IsNullOrWhiteSpace(token))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        return await base.SendAsync(request, cancellationToken);
    }
}
