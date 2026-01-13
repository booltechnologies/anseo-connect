using System.Security.Claims;
using AnseoConnect.Data;
using AnseoConnect.Data.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AnseoConnect.ApiGateway.Controllers;

[ApiController]
[Route("api/guardian/consent")]
[Authorize(Roles = "Guardian")]
public sealed class GuardianConsentController : ControllerBase
{
    private readonly AnseoConnectDbContext _dbContext;

    public GuardianConsentController(AnseoConnectDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpGet]
    public async Task<IActionResult> GetConsent(CancellationToken ct)
    {
        var guardianId = GetGuardianId();
        var consents = await _dbContext.ConsentStates
            .AsNoTracking()
            .Where(c => c.GuardianId == guardianId)
            .Select(c => new { c.Channel, c.State, c.Source, c.LastUpdatedUtc })
            .ToListAsync(ct);
        return Ok(consents);
    }

    [HttpPut]
    public async Task<IActionResult> UpdateConsent([FromBody] ConsentUpdate request, CancellationToken ct)
    {
        var guardianId = GetGuardianId();
        var guardian = await _dbContext.Guardians.AsNoTracking().FirstOrDefaultAsync(g => g.GuardianId == guardianId, ct);
        if (guardian == null) return Unauthorized();

        var state = await _dbContext.ConsentStates
            .FirstOrDefaultAsync(c => c.GuardianId == guardianId && c.Channel == request.Channel, ct);
        if (state == null)
        {
            state = new ConsentState
            {
                ConsentStateId = Guid.NewGuid(),
                GuardianId = guardianId,
                Channel = request.Channel,
                TenantId = guardian.TenantId,
                SchoolId = guardian.SchoolId
            };
            _dbContext.ConsentStates.Add(state);
        }
        state.State = request.State;
        state.Source = "GUARDIAN_PORTAL";
        state.LastUpdatedUtc = DateTimeOffset.UtcNow;

        var record = new ConsentRecord
        {
            ConsentRecordId = Guid.NewGuid(),
            GuardianId = guardianId,
            TenantId = state.TenantId,
            SchoolId = state.SchoolId,
            Channel = request.Channel,
            Action = request.State,
            Source = "GUARDIAN_PORTAL",
            CapturedAtUtc = DateTimeOffset.UtcNow
        };
        _dbContext.ConsentRecords.Add(record);

        await _dbContext.SaveChangesAsync(ct);
        return Accepted(new { request.Channel, request.State });
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

    public sealed record ConsentUpdate(string Channel, string State);
}
