using AnseoConnect.Contracts.Common;
using AnseoConnect.Contracts.Events;
using AnseoConnect.Shared;
using Microsoft.AspNetCore.Mvc;

namespace AnseoConnect.Ingestion.Wonde.Controllers;

[ApiController]
[Route("test")]
public sealed class TestController : ControllerBase
{
    private readonly IMessageBus _messageBus;
    private readonly ILogger<TestController> _logger;

    public TestController(IMessageBus messageBus, ILogger<TestController> logger)
    {
        _messageBus = messageBus;
        _logger = logger;
    }

    /// <summary>
    /// Test endpoint to publish a sample AttendanceMarksIngestedV1 message.
    /// POST /test/publish
    /// </summary>
    [HttpPost("publish")]
    public async Task<IActionResult> PublishTestMessage([FromBody] TestPublishRequest? request, CancellationToken cancellationToken)
    {
        // For testing - use default values if not provided
        var tenantId = request?.TenantId ?? Guid.Parse("11111111-1111-1111-1111-111111111111");
        var schoolId = request?.SchoolId ?? Guid.Parse("22222222-2222-2222-2222-222222222222");

        var payload = new AttendanceMarksIngestedV1(
            Date: request?.Date ?? DateOnly.FromDateTime(DateTime.UtcNow),
            StudentCount: request?.StudentCount ?? 10,
            MarkCount: request?.MarkCount ?? 20,
            Source: request?.Source ?? "WONDE"
        );

        var envelope = new MessageEnvelope<AttendanceMarksIngestedV1>(
            MessageType: MessageTypes.AttendanceMarksIngestedV1,
            Version: MessageVersions.V1,
            TenantId: tenantId,
            SchoolId: schoolId,
            CorrelationId: Guid.NewGuid().ToString(),
            OccurredAtUtc: DateTimeOffset.UtcNow,
            Payload: payload
        );

        try
        {
            await _messageBus.PublishAsync(envelope, cancellationToken);
            return Ok(new { message = "Message published successfully", correlationId = envelope.CorrelationId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish test message");
            return StatusCode(500, new { error = "Failed to publish message", details = ex.Message });
        }
    }
}

public sealed record TestPublishRequest(
    Guid? TenantId = null,
    Guid? SchoolId = null,
    DateOnly? Date = null,
    int? StudentCount = null,
    int? MarkCount = null,
    string? Source = null
);
