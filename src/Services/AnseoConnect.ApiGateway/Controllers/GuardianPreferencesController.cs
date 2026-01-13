using System.Security.Claims;
using AnseoConnect.Data;
using AnseoConnect.Data.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AnseoConnect.ApiGateway.Controllers;

[ApiController]
[Route("api/guardian/preferences")]
[Authorize(Roles = "Guardian")]
public sealed class GuardianPreferencesController : ControllerBase
{
    private readonly AnseoConnectDbContext _dbContext;

    public GuardianPreferencesController(AnseoConnectDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpGet]
    public async Task<IActionResult> GetPreferences(CancellationToken ct)
    {
        var guardianId = GetGuardianId();
        var pref = await _dbContext.ContactPreferences.AsNoTracking()
            .FirstOrDefaultAsync(p => p.GuardianId == guardianId, ct);

        if (pref == null)
        {
            return Ok(new
            {
                PreferredLanguage = "en",
                PreferredChannels = new[] { "SMS", "EMAIL" },
                QuietHoursJson = (string?)null
            });
        }

        return Ok(new
        {
            pref.PreferredLanguage,
            PreferredChannels = pref.PreferredChannelsJson,
            pref.QuietHoursJson
        });
    }

    [HttpPut]
    public async Task<IActionResult> UpdatePreferences([FromBody] ContactPreferenceUpdate request, CancellationToken ct)
    {
        var guardianId = GetGuardianId();
        var guardian = await _dbContext.Guardians.AsNoTracking().FirstOrDefaultAsync(g => g.GuardianId == guardianId, ct);
        if (guardian == null)
        {
            return Unauthorized();
        }
        var pref = await _dbContext.ContactPreferences
            .FirstOrDefaultAsync(p => p.GuardianId == guardianId, ct);

        if (pref == null)
        {
            pref = new ContactPreference
            {
                ContactPreferenceId = Guid.NewGuid(),
                GuardianId = guardianId,
                TenantId = guardian.TenantId,
                SchoolId = guardian.SchoolId
            };
            _dbContext.ContactPreferences.Add(pref);
        }

        pref.PreferredLanguage = request.PreferredLanguage;
        pref.PreferredChannelsJson = request.PreferredChannelsJson;
        pref.QuietHoursJson = request.QuietHoursJson;
        pref.UpdatedAtUtc = DateTimeOffset.UtcNow;

        await _dbContext.SaveChangesAsync(ct);
        return Accepted(new { guardianId, pref.PreferredLanguage });
    }

    private Guid GetGuardianId()
    {
        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (Guid.TryParse(sub, out var id))
        {
            return id;
        }
        throw new UnauthorizedAccessException("Guardian id missing.");
    }

    public sealed record ContactPreferenceUpdate(string PreferredLanguage, string PreferredChannelsJson, string? QuietHoursJson);
}
