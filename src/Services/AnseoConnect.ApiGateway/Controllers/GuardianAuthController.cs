using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using AnseoConnect.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.IdentityModel.Tokens;
using AnseoConnect.ApiGateway.Services;

namespace AnseoConnect.ApiGateway.Controllers;

[ApiController]
[Route("api/guardian/auth")]
public sealed class GuardianAuthController : ControllerBase
{
    private readonly AnseoConnectDbContext _dbContext;
    private readonly IConfiguration _configuration;
    private readonly IMemoryCache _cache;
    private readonly ILogger<GuardianAuthController> _logger;
    private readonly IEmailSender? _emailSender;
    private readonly ISmsSender? _smsSender;
    private const string CookieName = "guardian_auth";
    private static readonly TimeSpan ThrottleWindow = TimeSpan.FromSeconds(30);

    public GuardianAuthController(
        AnseoConnectDbContext dbContext,
        IConfiguration configuration,
        IMemoryCache cache,
        ILogger<GuardianAuthController> logger,
        IEmailSender? emailSender,
        ISmsSender? smsSender)
    {
        _dbContext = dbContext;
        _configuration = configuration;
        _cache = cache;
        _logger = logger;
        _emailSender = emailSender;
        _smsSender = smsSender;
    }

    [HttpPost("magic-link")]
    public async Task<IActionResult> RequestMagicLink([FromBody] MagicLinkRequest request, CancellationToken ct)
    {
        var throttleKey = $"magiclink:{request.EmailOrPhone}";
        if (_cache.TryGetValue<DateTimeOffset>(throttleKey, out var lastIssued) &&
            DateTimeOffset.UtcNow - lastIssued < ThrottleWindow)
        {
            _logger.LogWarning("Magic link throttled for {Identifier}", request.EmailOrPhone);
            return StatusCode(StatusCodes.Status429TooManyRequests, "Too many requests. Please wait.");
        }

        var guardian = await _dbContext.Guardians
            .AsNoTracking()
            .FirstOrDefaultAsync(g => g.Email == request.EmailOrPhone || g.MobileE164 == request.EmailOrPhone, ct);

        if (guardian == null)
        {
            return NotFound();
        }

        var nonce = Guid.NewGuid().ToString();
        _cache.Set($"magic-nonce:{guardian.GuardianId}", nonce, TimeSpan.FromMinutes(15));
        var token = CreateOneTimeToken(guardian.GuardianId, nonce, expiresMinutes: 15);

        string? otp = null;
        if (!string.IsNullOrWhiteSpace(guardian.MobileE164))
        {
            otp = GenerateOtp();
            _cache.Set($"otp:{guardian.GuardianId}", otp, TimeSpan.FromMinutes(10));
        }

        _logger.LogInformation("Issued magic link token for guardian {GuardianId}", guardian.GuardianId);
        _cache.Set(throttleKey, DateTimeOffset.UtcNow, ThrottleWindow);

        var baseUrl = _configuration["Guardian:MagicLinkBaseUrl"];
        var link = string.IsNullOrWhiteSpace(baseUrl)
            ? token
            : $"{baseUrl.TrimEnd('/')}/guardian/login?token={Uri.EscapeDataString(token)}";

        if (_emailSender != null && !string.IsNullOrWhiteSpace(guardian.Email))
        {
            var subject = "Your Anseo Connect sign-in link";
            var html = $"<p>Click to sign in: <a href=\"{link}\">{link}</a></p>";
            await _emailSender.SendAsync(guardian.Email, subject, html, link, ct);
        }

        if (_smsSender != null && !string.IsNullOrWhiteSpace(guardian.MobileE164) && otp != null)
        {
            await _smsSender.SendAsync(guardian.MobileE164, $"Your Anseo Connect OTP is {otp}", ct);
        }

        return Accepted(new { guardianId = guardian.GuardianId, expiresAt = DateTimeOffset.UtcNow.AddMinutes(15) });
    }

    [HttpPost("verify")]
    public async Task<IActionResult> VerifyToken([FromBody] MagicLinkVerify request, CancellationToken ct)
    {
        Guid guardianId;
        if (!string.IsNullOrWhiteSpace(request.Token))
        {
            var principal = ValidateOneTimeToken(request.Token);
            if (principal == null)
            {
                return Unauthorized();
            }
            var sub = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!Guid.TryParse(sub, out guardianId))
            {
                return Unauthorized();
            }
        }
        else if (request.GuardianId.HasValue && !string.IsNullOrWhiteSpace(request.Otp))
        {
            guardianId = request.GuardianId.Value;
            if (!ValidateOtp(guardianId, request.Otp))
            {
                return Unauthorized();
            }
        }
        else
        {
            return BadRequest("Token or guardianId+otp required.");
        }

        var guardian = await _dbContext.Guardians.AsNoTracking().FirstOrDefaultAsync(g => g.GuardianId == guardianId, ct);
        if (guardian == null)
        {
            return Unauthorized();
        }

        var authToken = CreateGuardianAuthToken(guardian);
        Response.Cookies.Append(CookieName, authToken, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Lax,
            Expires = DateTimeOffset.UtcNow.AddDays(7)
        });

        return Ok(new { token = authToken });
    }

    private string CreateOneTimeToken(Guid guardianId, string nonce, int expiresMinutes)
    {
        var key = GetJwtKey();
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var handler = new JwtSecurityTokenHandler();
        var token = handler.CreateJwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"] ?? "AnseoConnect",
            audience: _configuration["Jwt:Audience"] ?? "AnseoConnect",
            subject: new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, guardianId.ToString()),
                new Claim("typ", "guardian_magic"),
                new Claim("nonce", nonce)
            }),
            expires: DateTime.UtcNow.AddMinutes(expiresMinutes),
            signingCredentials: creds);
        return handler.WriteToken(token);
    }

    private ClaimsPrincipal? ValidateOneTimeToken(string token)
    {
        var handler = new JwtSecurityTokenHandler();
        try
        {
            var principal = handler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = _configuration["Jwt:Issuer"] ?? "AnseoConnect",
                ValidAudience = _configuration["Jwt:Audience"] ?? "AnseoConnect",
                IssuerSigningKey = GetJwtKey(),
                ClockSkew = TimeSpan.Zero
            }, out _);

            var sub = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var nonce = principal.FindFirst("nonce")?.Value;
            if (!string.IsNullOrWhiteSpace(sub) && !string.IsNullOrWhiteSpace(nonce))
            {
                var cacheKey = $"magic-nonce:{sub}";
                if (_cache.TryGetValue<string>(cacheKey, out var cachedNonce) && cachedNonce == nonce)
                {
                    return principal;
                }
            }
            _logger.LogWarning("Magic link nonce invalid");
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Invalid magic link token");
            return null;
        }
    }

    private string CreateGuardianAuthToken(Data.Entities.Guardian guardian)
    {
        var key = GetJwtKey();
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var handler = new JwtSecurityTokenHandler();
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, guardian.GuardianId.ToString()),
            new Claim("tenant_id", guardian.TenantId.ToString()),
            new Claim("school_id", guardian.SchoolId.ToString()),
            new Claim(ClaimTypes.Role, "Guardian")
        };
        var token = handler.CreateJwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"] ?? "AnseoConnect",
            audience: _configuration["Jwt:Audience"] ?? "AnseoConnect",
            subject: new ClaimsIdentity(claims),
            expires: DateTime.UtcNow.AddDays(7),
            signingCredentials: creds);
        return handler.WriteToken(token);
    }

    private SymmetricSecurityKey GetJwtKey()
    {
        var secret = _configuration["Jwt:Secret"] ?? Environment.GetEnvironmentVariable("ANSEO_JWT_SECRET")
            ?? throw new InvalidOperationException("JWT secret not configured");
        return new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
    }

    private static string GenerateOtp()
    {
        var rnd = RandomNumberGenerator.GetInt32(100000, 999999);
        return rnd.ToString();
    }

    private bool ValidateOtp(Guid guardianId, string otp)
    {
        if (_cache.TryGetValue<string>($"otp:{guardianId}", out var cached) && cached == otp)
        {
            _cache.Remove($"otp:{guardianId}");
            return true;
        }
        return false;
    }

    public sealed record MagicLinkRequest(string EmailOrPhone);
    public sealed record MagicLinkVerify(string? Token, Guid? GuardianId, string? Otp);
}
