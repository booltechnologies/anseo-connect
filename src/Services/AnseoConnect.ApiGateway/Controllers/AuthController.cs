using AnseoConnect.Data;
using AnseoConnect.Data.Entities;
using AnseoConnect.Data.MultiTenancy;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace AnseoConnect.ApiGateway.Controllers;

[ApiController]
[Route("auth")]
public sealed class AuthController : ControllerBase
{
    private readonly UserManager<AppUser> _userManager;
    private readonly SignInManager<AppUser> _signInManager;
    private readonly IConfiguration _configuration;
    private readonly ITenantContext _tenantContext;

    public AuthController(
        UserManager<AppUser> userManager,
        SignInManager<AppUser> signInManager,
        IConfiguration configuration,
        ITenantContext tenantContext)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _configuration = configuration;
        _tenantContext = tenantContext;
    }

    /// <summary>
    /// Local login endpoint for staff users.
    /// POST /auth/local/login
    /// </summary>
    [HttpPost("local/login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
        {
            return BadRequest(new { error = "Username and password are required." });
        }

        // Find user by username (AppUser is not filtered by tenant, but is scoped via unique index)
        // If tenantId is provided in request, we can optionally validate it matches
        AppUser? user = null;
        
        // Try to find user by username first
        user = await _userManager.FindByNameAsync(request.Username);
        
        // If tenant is provided, validate it matches
        if (user != null && request.TenantId.HasValue && user.TenantId != request.TenantId.Value)
        {
            // Username exists but in different tenant - return generic error
            return Unauthorized(new { error = "Invalid username or password." });
        }

        if (user == null)
        {
            // Return generic error to avoid username enumeration
            return Unauthorized(new { error = "Invalid username or password." });
        }

        if (!user.IsActive)
        {
            return Unauthorized(new { error = "Account is inactive." });
        }

        var result = await _signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: false);
        if (!result.Succeeded)
        {
            return Unauthorized(new { error = "Invalid username or password." });
        }

        // Generate JWT token
        var token = GenerateJwtToken(user);

        return Ok(new LoginResponse(
            Token: token,
            UserId: user.Id,
            TenantId: user.TenantId,
            SchoolId: user.SchoolId,
            Username: user.UserName!,
            Email: user.Email
        ));
    }

    /// <summary>
    /// Get current user information.
    /// GET /auth/whoami
    /// </summary>
    [HttpGet("whoami")]
    [Authorize]
    public async Task<IActionResult> WhoAmI()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;

        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized();
        }

        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null)
        {
            return NotFound();
        }

        var tenantId = User.FindFirst("tenant_id")?.Value;
        var schoolId = User.FindFirst("school_id")?.Value;

        return Ok(new WhoAmIResponse(
            UserId: user.Id,
            TenantId: Guid.TryParse(tenantId, out var t) ? t : user.TenantId,
            SchoolId: Guid.TryParse(schoolId, out var s) ? (Guid?)s : user.SchoolId,
            Username: user.UserName!,
            Email: user.Email,
            FirstName: user.FirstName,
            LastName: user.LastName,
            Claims: User.Claims.Select(c => (object)new { c.Type, c.Value }).ToList()
        ));
    }

    private string GenerateJwtToken(AppUser user)
    {
        var secret = _configuration["Jwt:Secret"] 
            ?? Environment.GetEnvironmentVariable("ANSEO_JWT_SECRET")
            ?? throw new InvalidOperationException("JWT secret not configured.");
        
        var key = Encoding.UTF8.GetBytes(secret);
        var issuer = _configuration["Jwt:Issuer"] ?? "AnseoConnect";
        var audience = _configuration["Jwt:Audience"] ?? "AnseoConnect";
        var expiresInMinutes = int.Parse(_configuration["Jwt:ExpiresInMinutes"] ?? "480"); // 8 hours default

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.UserName!),
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new("tenant_id", user.TenantId.ToString()),
            new("school_id", user.SchoolId.ToString())
        };

        if (!string.IsNullOrEmpty(user.Email))
        {
            claims.Add(new Claim(ClaimTypes.Email, user.Email));
        }

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddMinutes(expiresInMinutes),
            Issuer = issuer,
            Audience = audience,
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }
}

public sealed record LoginRequest(string Username, string Password, Guid? TenantId = null);

public sealed record LoginResponse(
    string Token,
    Guid UserId,
    Guid TenantId,
    Guid SchoolId,
    string? Username,
    string? Email
);

public sealed record WhoAmIResponse(
    Guid UserId,
    Guid TenantId,
    Guid? SchoolId,
    string? Username,
    string? Email,
    string FirstName,
    string LastName,
    List<object> Claims
);
