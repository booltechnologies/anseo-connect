using System.Security.Claims;
using AnseoConnect.Client;
using AnseoConnect.Client.Models;
using Microsoft.AspNetCore.Components.Authorization;

namespace AnseoConnect.Web.Security;

public sealed class SessionState
{
    private LoginResponse? _login;
    private WhoAmIResponse? _whoAmI;

    public string? AccessToken => _login?.Token;
    public WhoAmIResponse? User => _whoAmI;
    public bool IsAuthenticated => _login != null;

    public event Action? StateChanged;

    public void SetSession(LoginResponse login, WhoAmIResponse whoAmI)
    {
        _login = login;
        _whoAmI = whoAmI;
        StateChanged?.Invoke();
    }

    public void Clear()
    {
        _login = null;
        _whoAmI = null;
        StateChanged?.Invoke();
    }

    public ClaimsPrincipal ToPrincipal()
    {
        if (_login == null || _whoAmI == null)
        {
            return new ClaimsPrincipal(new ClaimsIdentity());
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, _whoAmI.UserId.ToString()),
            new(ClaimTypes.Name, _whoAmI.Username ?? string.Empty),
            new("tenant_id", _whoAmI.TenantId.ToString())
        };

        if (_whoAmI.SchoolId.HasValue)
        {
            claims.Add(new Claim("school_id", _whoAmI.SchoolId.Value.ToString()));
        }

        if (!string.IsNullOrWhiteSpace(_whoAmI.Email))
        {
            claims.Add(new Claim(ClaimTypes.Email, _whoAmI.Email!));
        }

        var identity = new ClaimsIdentity(claims, authenticationType: "Bearer");
        return new ClaimsPrincipal(identity);
    }
}

public sealed class ClientTokenProvider : IClientTokenProvider
{
    private readonly SessionState _sessionState;

    public ClientTokenProvider(SessionState sessionState)
    {
        _sessionState = sessionState;
    }

    public ValueTask<string?> GetTokenAsync(CancellationToken cancellationToken = default)
    {
        return ValueTask.FromResult(_sessionState.AccessToken);
    }
}

public sealed class JwtAuthenticationStateProvider : AuthenticationStateProvider, IDisposable
{
    private readonly SessionState _sessionState;

    public JwtAuthenticationStateProvider(SessionState sessionState)
    {
        _sessionState = sessionState;
        _sessionState.StateChanged += NotifyAuthStateChanged;
    }

    public override Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        var principal = _sessionState.ToPrincipal();
        return Task.FromResult(new AuthenticationState(principal));
    }

    private void NotifyAuthStateChanged() => NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());

    public void Dispose()
    {
        _sessionState.StateChanged -= NotifyAuthStateChanged;
    }
}
