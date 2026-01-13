using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using AnseoConnect.Client.Models;

namespace AnseoConnect.Client;

public sealed class LettersClient : ApiClientBase
{
    public LettersClient(HttpClient httpClient, IOptions<ApiClientOptions> options, ILogger<LettersClient> logger)
        : base(httpClient, options, logger)
    {
    }

    public Task<List<LetterTemplateDto>?> GetTemplatesAsync(CancellationToken cancellationToken = default)
        => GetOrDefaultAsync<List<LetterTemplateDto>>("api/letters/templates", cancellationToken);

    public Task<LetterArtifactDto?> GenerateLetterAsync(GenerateLetterRequest request, CancellationToken cancellationToken = default)
        => PostOrDefaultAsync<GenerateLetterRequest, LetterArtifactDto>("api/letters/generate", request, cancellationToken);
}

