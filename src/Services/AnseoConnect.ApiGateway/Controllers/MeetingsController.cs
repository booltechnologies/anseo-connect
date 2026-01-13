using AnseoConnect.Data;
using AnseoConnect.Workflow.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AnseoConnect.ApiGateway.Controllers;

[ApiController]
[Route("api/meetings")]
[Authorize(Policy = "StaffOnly")]
public sealed class MeetingsController : ControllerBase
{
    private readonly AnseoConnectDbContext _dbContext;
    private readonly MeetingService _meetingService;

    public MeetingsController(
        AnseoConnectDbContext dbContext,
        MeetingService meetingService)
    {
        _dbContext = dbContext;
        _meetingService = meetingService;
    }

    [HttpGet]
    public async Task<IActionResult> GetMeetings(CancellationToken cancellationToken)
    {
        var meetings = await _dbContext.InterventionMeetings.AsNoTracking().ToListAsync(cancellationToken);
        var instanceIds = meetings.Select(m => m.InstanceId).Distinct().ToList();
        var stageIds = meetings.Select(m => m.StageId).Distinct().ToList();

        var instances = await _dbContext.StudentInterventionInstances
            .AsNoTracking()
            .Where(i => instanceIds.Contains(i.InstanceId))
            .ToDictionaryAsync(i => i.InstanceId, cancellationToken);

        var studentIds = instances.Values.Select(i => i.StudentId).Distinct().ToList();
        var students = await _dbContext.Students
            .AsNoTracking()
            .Where(s => studentIds.Contains(s.StudentId))
            .Select(s => new
            {
                s.StudentId,
                Name = (s.FirstName + " " + s.LastName).Trim()
            })
            .ToDictionaryAsync(s => s.StudentId, cancellationToken);

        var stages = await _dbContext.InterventionStages
            .AsNoTracking()
            .Where(s => stageIds.Contains(s.StageId))
            .ToDictionaryAsync(s => s.StageId, cancellationToken);

        var result = meetings.Select(m =>
        {
            instances.TryGetValue(m.InstanceId, out var instance);
            var studentId = instance?.StudentId ?? Guid.Empty;
            students.TryGetValue(studentId, out var student);
            stages.TryGetValue(m.StageId, out var stage);

            return new
            {
                m.MeetingId,
                m.InstanceId,
                m.StageId,
                StageName = stage?.StageType ?? string.Empty,
                StudentId = studentId,
                StudentName = student?.Name ?? "Unknown",
                m.ScheduledAtUtc,
                m.HeldAtUtc,
                m.Status,
                m.AttendeesJson,
                m.OutcomeCode,
                m.OutcomeNotes
            };
        }).ToList();

        return Ok(result);
    }

    public sealed record ScheduleMeetingRequest(Guid InstanceId, Guid StageId, DateTimeOffset ScheduledAtUtc, string? AttendeesJson);

    [HttpPost]
    public async Task<IActionResult> Schedule([FromBody] ScheduleMeetingRequest request, CancellationToken cancellationToken)
    {
        var meeting = await _meetingService.ScheduleAsync(
            request.InstanceId,
            request.StageId,
            request.ScheduledAtUtc,
            null,
            request.AttendeesJson,
            cancellationToken);

        return Ok(meeting);
    }

    public sealed record MeetingOutcomeRequest(string Status, string? OutcomeCode, string? OutcomeNotes, string? NotesJson);

    [HttpPut("{meetingId:guid}")]
    public async Task<IActionResult> UpdateOutcome(Guid meetingId, [FromBody] MeetingOutcomeRequest request, CancellationToken cancellationToken)
    {
        var meeting = await _meetingService.RecordOutcomeAsync(
            meetingId,
            request.Status,
            request.OutcomeCode,
            request.OutcomeNotes,
            request.NotesJson,
            cancellationToken);

        return Ok(meeting);
    }
}

