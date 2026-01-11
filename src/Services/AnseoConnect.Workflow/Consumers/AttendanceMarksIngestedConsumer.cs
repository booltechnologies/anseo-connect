using AnseoConnect.Contracts.Commands;
using AnseoConnect.Contracts.Common;
using AnseoConnect.Contracts.Events;
using AnseoConnect.Data;
using AnseoConnect.Shared;
using AnseoConnect.Workflow.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace AnseoConnect.Workflow.Consumers;

/// <summary>
/// Consumer for AttendanceMarksIngestedV1 messages from the attendance topic.
/// </summary>
public sealed class AttendanceMarksIngestedConsumer : ServiceBusMessageConsumer
{
    public AttendanceMarksIngestedConsumer(
        string connectionString,
        IServiceProvider serviceProvider,
        ILogger<AttendanceMarksIngestedConsumer> logger)
        : base(connectionString, "attendance", "workflow-attendance-ingested", serviceProvider, logger)
    {
    }

    protected override async Task ProcessMessageAsync(
        string messageType,
        string version,
        Guid tenantId,
        Guid? schoolId,
        string correlationId,
        DateTimeOffset occurredAtUtc,
        string payloadJson,
        IServiceScope scope,
        CancellationToken cancellationToken)
    {
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<AttendanceMarksIngestedConsumer>>();

        logger.LogInformation(
            "Received message {MessageType} v{Version} for tenant {TenantId}, school {SchoolId}, correlation {CorrelationId}",
            messageType,
            version,
            tenantId,
            schoolId,
            correlationId);

        if (messageType == MessageTypes.AttendanceMarksIngestedV1 && version == MessageVersions.V1)
        {
            var payload = JsonSerializer.Deserialize<AttendanceMarksIngestedV1>(payloadJson, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (payload != null && schoolId.HasValue)
            {
                logger.LogInformation(
                    "Processing AttendanceMarksIngestedV1: Date={Date}, Students={StudentCount}, Marks={MarkCount}, Source={Source}",
                    payload.Date,
                    payload.StudentCount,
                    payload.MarkCount,
                    payload.Source);

                var absenceDetectionService = scope.ServiceProvider.GetRequiredService<AbsenceDetectionService>();
                var caseService = scope.ServiceProvider.GetRequiredService<CaseService>();
                var safeguardingService = scope.ServiceProvider.GetRequiredService<SafeguardingService>();
                var messageBus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

                // Detect unexplained absences for the ingested date
                var unexplainedAbsences = await absenceDetectionService.DetectUnexplainedAbsencesAsync(
                    schoolId.Value,
                    payload.Date,
                    cancellationToken: cancellationToken);

                foreach (var absence in unexplainedAbsences)
                {
                    try
                    {
                        // Get or create attendance case
                        var attendanceCase = await caseService.GetOrCreateAttendanceCaseAsync(
                            absence.StudentId,
                            cancellationToken);

                        // Add timeline event
                        await caseService.AddTimelineEventAsync(
                            attendanceCase.CaseId,
                            "ABSENCE_DETECTED",
                            JsonSerializer.Serialize(new { absence.Date, absence.Session }),
                            null,
                            cancellationToken);

                        // Get primary guardian (first student-guardian relationship)
                        var dbContext = scope.ServiceProvider.GetRequiredService<AnseoConnectDbContext>();
                        var studentGuardian = await dbContext.StudentGuardians
                            .Where(sg => sg.StudentId == absence.StudentId)
                            .OrderBy(sg => sg.GuardianId) // Simple ordering - in production, check IsPrimary flag
                            .FirstOrDefaultAsync(cancellationToken);

                        if (studentGuardian != null)
                        {
                            // Get guardian to check available contact methods
                            var guardian = await dbContext.Guardians
                                .AsNoTracking()
                                .Where(g => g.GuardianId == studentGuardian.GuardianId)
                                .FirstOrDefaultAsync(cancellationToken);

                            if (guardian != null)
                            {
                                var templateData = new Dictionary<string, string>
                                {
                                    { "StudentName", absence.StudentName },
                                    { "Date", absence.Date.ToString("d") },
                                    { "Session", absence.Session }
                                };

                                // Send SMS command if guardian has mobile number
                                if (!string.IsNullOrWhiteSpace(guardian.MobileE164))
                                {
                                    var smsMessageRequest = new SendMessageRequestedV1(
                                        CaseId: attendanceCase.CaseId,
                                        StudentId: absence.StudentId,
                                        GuardianId: studentGuardian.GuardianId,
                                        Channel: "SMS",
                                        MessageType: "SERVICE_ATTENDANCE",
                                        TemplateId: "attendance-absence-v1",
                                        TemplateData: templateData
                                    );

                                    var smsEnvelope = new MessageEnvelope<SendMessageRequestedV1>(
                                        MessageType: MessageTypes.SendMessageRequestedV1,
                                        Version: MessageVersions.V1,
                                        TenantId: tenantId,
                                        SchoolId: schoolId.Value,
                                        CorrelationId: Guid.NewGuid().ToString(),
                                        OccurredAtUtc: DateTimeOffset.UtcNow,
                                        Payload: smsMessageRequest
                                    );

                                    await messageBus.PublishAsync(smsEnvelope, cancellationToken);

                                    logger.LogInformation(
                                        "Published SMS message request for student {StudentId}, guardian {GuardianId}, case {CaseId}",
                                        absence.StudentId,
                                        studentGuardian.GuardianId,
                                        attendanceCase.CaseId);
                                }

                                // Send EMAIL command if guardian has email address
                                if (!string.IsNullOrWhiteSpace(guardian.Email))
                                {
                                    var emailMessageRequest = new SendMessageRequestedV1(
                                        CaseId: attendanceCase.CaseId,
                                        StudentId: absence.StudentId,
                                        GuardianId: studentGuardian.GuardianId,
                                        Channel: "EMAIL",
                                        MessageType: "SERVICE_ATTENDANCE",
                                        TemplateId: "attendance-absence-v1",
                                        TemplateData: templateData
                                    );

                                    var emailEnvelope = new MessageEnvelope<SendMessageRequestedV1>(
                                        MessageType: MessageTypes.SendMessageRequestedV1,
                                        Version: MessageVersions.V1,
                                        TenantId: tenantId,
                                        SchoolId: schoolId.Value,
                                        CorrelationId: Guid.NewGuid().ToString(),
                                        OccurredAtUtc: DateTimeOffset.UtcNow,
                                        Payload: emailMessageRequest
                                    );

                                    await messageBus.PublishAsync(emailEnvelope, cancellationToken);

                                    logger.LogInformation(
                                        "Published EMAIL message request for student {StudentId}, guardian {GuardianId}, case {CaseId}",
                                        absence.StudentId,
                                        studentGuardian.GuardianId,
                                        attendanceCase.CaseId);
                                }
                            }
                        }

                        // Evaluate safeguarding triggers after message request is published
                        var safeguardingAlert = await safeguardingService.EvaluateAndCreateAlertAsync(
                            attendanceCase.CaseId,
                            absence.StudentId,
                            cancellationToken);

                        if (safeguardingAlert != null)
                        {
                            // Publish safeguarding alert event
                            var alertPayload = new SafeguardingAlertCreatedV1(
                                AlertId: safeguardingAlert.AlertId,
                                CaseId: attendanceCase.CaseId,
                                StudentId: absence.StudentId,
                                Severity: safeguardingAlert.Severity,
                                ChecklistId: safeguardingAlert.ChecklistId,
                                RequiresHumanReview: safeguardingAlert.RequiresHumanReview
                            );

                            var alertEnvelope = new MessageEnvelope<SafeguardingAlertCreatedV1>(
                                MessageType: MessageTypes.SafeguardingAlertCreatedV1,
                                Version: MessageVersions.V1,
                                TenantId: tenantId,
                                SchoolId: schoolId.Value,
                                CorrelationId: Guid.NewGuid().ToString(),
                                OccurredAtUtc: DateTimeOffset.UtcNow,
                                Payload: alertPayload
                            );

                            await messageBus.PublishAsync(alertEnvelope, cancellationToken);

                            logger.LogInformation(
                                "Created safeguarding alert {AlertId} for case {CaseId}",
                                safeguardingAlert.AlertId,
                                attendanceCase.CaseId);
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Error processing absence for student {StudentId}", absence.StudentId);
                        // Continue with next absence - don't throw to prevent message from being dead-lettered
                    }
                }

                logger.LogInformation(
                    "Processed {Count} unexplained absences for date {Date}",
                    unexplainedAbsences.Count,
                    payload.Date);
            }
            else
            {
                logger.LogWarning("Failed to deserialize AttendanceMarksIngestedV1 payload or missing schoolId. CorrelationId: {CorrelationId}", correlationId);
            }
        }
        else
        {
            logger.LogWarning("Unknown message type {MessageType} v{Version}. CorrelationId: {CorrelationId}", messageType, version, correlationId);
        }
    }
}
