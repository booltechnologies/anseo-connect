using AnseoConnect.Contracts.Common;
using AnseoConnect.Contracts.Events;
using AnseoConnect.Data;
using AnseoConnect.Data.Entities;
using AnseoConnect.Data.MultiTenancy;
using AnseoConnect.Ingestion.Wonde.Client;
using AnseoConnect.Shared;
using Microsoft.EntityFrameworkCore;

namespace AnseoConnect.Ingestion.Wonde.Services;

/// <summary>
/// Service that orchestrates ingestion of data from Wonde API.
/// </summary>
public sealed class IngestionService
{
    private readonly IWondeClient _wondeClient;
    private readonly AnseoConnectDbContext _dbContext;
    private readonly IMessageBus _messageBus;
    private readonly ILogger<IngestionService> _logger;
    private readonly ITenantContext _tenantContext;

    public IngestionService(
        IWondeClient wondeClient,
        AnseoConnectDbContext dbContext,
        IMessageBus messageBus,
        ILogger<IngestionService> logger,
        ITenantContext tenantContext)
    {
        _wondeClient = wondeClient;
        _dbContext = dbContext;
        _messageBus = messageBus;
        _logger = logger;
        _tenantContext = tenantContext;
    }

    /// <summary>
    /// Runs ingestion for a specific school and date.
    /// </summary>
    public async Task<IngestionResult> RunIngestionAsync(Guid schoolId, DateOnly date, CancellationToken cancellationToken = default)
    {
        var startTime = DateTimeOffset.UtcNow;
        _logger.LogInformation("Starting ingestion for school {SchoolId}, date {Date}", schoolId, date);

        // First, query school without tenant filtering (School entity is not filtered, only school-scoped entities are)
        // We need to find the school first to get TenantId, then set TenantContext
        // Load as tracked so we can update LastSyncUtc after ingestion
        var school = await _dbContext.Schools
            .Where(s => s.SchoolId == schoolId)
            .FirstOrDefaultAsync(cancellationToken);

        if (school == null)
        {
            throw new InvalidOperationException($"School {schoolId} not found.");
        }

        if (string.IsNullOrEmpty(school.WondeSchoolId))
        {
            throw new InvalidOperationException($"School {schoolId} does not have WondeSchoolId configured.");
        }

        // Set tenant context for subsequent DB operations
        if (_tenantContext is TenantContext tc)
        {
            tc.Set(school.TenantId, school.SchoolId);
        }

        // For v0.1, use configured default domain (will be enhanced later to get domain from school API)
        _logger.LogInformation("Starting ingestion for school {SchoolId} using Wonde API", schoolId);

        var result = new IngestionResult
        {
            SchoolId = schoolId,
            Date = date,
            StartTimeUtc = startTime
        };

        try
        {
            // Ingest students
            var studentsResponse = await _wondeClient.GetStudentsAsync(school.WondeSchoolId, cancellationToken: cancellationToken);
            if (studentsResponse?.Data != null)
            {
                result.StudentCount = await UpsertStudentsAsync(studentsResponse.Data, cancellationToken);
            }

            // Ingest guardians (contacts)
            var contactsResponse = await _wondeClient.GetContactsAsync(school.WondeSchoolId, cancellationToken: cancellationToken);
            if (contactsResponse?.Data != null)
            {
                result.GuardianCount = await UpsertGuardiansAsync(contactsResponse.Data, cancellationToken);
                
                // Update student-guardian relationships
                if (studentsResponse?.Data != null)
                {
                    await UpsertStudentGuardianRelationshipsAsync(studentsResponse.Data, contactsResponse.Data, cancellationToken);
                }
            }

            // Ingest attendance marks for the specified date
            var attendanceResponse = await _wondeClient.GetAttendanceAsync(school.WondeSchoolId, date, cancellationToken);
            if (attendanceResponse?.Data != null)
            {
                result.MarkCount = await UpsertAttendanceMarksAsync(attendanceResponse.Data, date, cancellationToken);
            }

            result.EndTimeUtc = DateTimeOffset.UtcNow;
            result.Duration = result.EndTimeUtc - result.StartTimeUtc;
            result.Success = true;

            // Update LastSyncUtc on school entity for incremental sync
            school.LastSyncUtc = DateTimeOffset.UtcNow;
            await _dbContext.SaveChangesAsync(cancellationToken);

            // Publish event if marks were ingested
            if (result.MarkCount > 0)
            {
                await PublishIngestionEventAsync(date, result.StudentCount, result.MarkCount, cancellationToken);
            }

            _logger.LogInformation(
                "Ingestion completed for school {SchoolId}, date {Date}. Students: {StudentCount}, Guardians: {GuardianCount}, Marks: {MarkCount}, Duration: {Duration}ms",
                schoolId,
                date,
                result.StudentCount,
                result.GuardianCount,
                result.MarkCount,
                result.Duration.TotalMilliseconds);

            return result;
        }
        catch (Exception ex)
        {
            result.EndTimeUtc = DateTimeOffset.UtcNow;
            result.Duration = result.EndTimeUtc - result.StartTimeUtc;
            result.Success = false;
            result.ErrorMessage = ex.Message;

            _logger.LogError(ex, "Ingestion failed for school {SchoolId}, date {Date}", schoolId, date);
            throw;
        }
    }

    private async Task<int> UpsertStudentsAsync(List<WondeStudent> wondeStudents, CancellationToken cancellationToken)
    {
        var upserted = 0;

        foreach (var wondeStudent in wondeStudents)
        {
            try
            {
                var existing = await _dbContext.Students
                    .Where(s => s.ExternalStudentId == wondeStudent.Id)
                    .FirstOrDefaultAsync(cancellationToken);

                if (existing == null)
                {
                    existing = new Student
                    {
                        ExternalStudentId = wondeStudent.Id,
                        FirstName = wondeStudent.Forename ?? "",
                        LastName = wondeStudent.Surname ?? "",
                        YearGroup = wondeStudent.YearGroup?.Name,
                        IsActive = wondeStudent.Active ?? true
                    };
                    _dbContext.Students.Add(existing);
                }
                else
                {
                    existing.FirstName = wondeStudent.Forename ?? existing.FirstName;
                    existing.LastName = wondeStudent.Surname ?? existing.LastName;
                    existing.YearGroup = wondeStudent.YearGroup?.Name ?? existing.YearGroup;
                    existing.IsActive = wondeStudent.Active ?? existing.IsActive;
                }

                upserted++;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to upsert student {StudentId}", wondeStudent.Id);
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        return upserted;
    }

    private async Task<int> UpsertGuardiansAsync(List<WondeContact> wondeContacts, CancellationToken cancellationToken)
    {
        var upserted = 0;

        foreach (var wondeContact in wondeContacts)
        {
            try
            {
                var existing = await _dbContext.Guardians
                    .Where(g => g.ExternalGuardianId == wondeContact.Id)
                    .FirstOrDefaultAsync(cancellationToken);

                var fullName = $"{wondeContact.Forename ?? ""} {wondeContact.Surname ?? ""}".Trim();
                var mobileE164 = wondeContact.ContactDetails?
                    .FirstOrDefault(cd => cd.Type.Equals("mobile", StringComparison.OrdinalIgnoreCase))?.Value;
                var email = wondeContact.ContactDetails?
                    .FirstOrDefault(cd => cd.Type.Equals("email", StringComparison.OrdinalIgnoreCase))?.Value;

                if (existing == null)
                {
                    existing = new Guardian
                    {
                        ExternalGuardianId = wondeContact.Id,
                        FullName = fullName,
                        MobileE164 = mobileE164,
                        Email = email,
                        IsActive = true
                    };
                    _dbContext.Guardians.Add(existing);
                }
                else
                {
                    existing.FullName = string.IsNullOrWhiteSpace(fullName) ? existing.FullName : fullName;
                    existing.MobileE164 = mobileE164 ?? existing.MobileE164;
                    existing.Email = email ?? existing.Email;
                }

                upserted++;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to upsert guardian {GuardianId}", wondeContact.Id);
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        return upserted;
    }

    private async Task<int> UpsertStudentGuardianRelationshipsAsync(
        List<WondeStudent> wondeStudents,
        List<WondeContact> wondeContacts,
        CancellationToken cancellationToken)
    {
        var contactMap = wondeContacts.ToDictionary(c => c.Id, c => c);
        var upserted = 0;

        foreach (var wondeStudent in wondeStudents)
        {
            if (wondeStudent.Contacts == null || wondeStudent.Contacts.Count == 0)
            {
                continue;
            }

            var student = await _dbContext.Students
                .Where(s => s.ExternalStudentId == wondeStudent.Id)
                .FirstOrDefaultAsync(cancellationToken);

            if (student == null)
            {
                continue;
            }

            foreach (var contact in wondeStudent.Contacts)
            {
                try
                {
                    var guardian = await _dbContext.Guardians
                        .Where(g => g.ExternalGuardianId == contact.Id)
                        .FirstOrDefaultAsync(cancellationToken);

                    if (guardian == null)
                    {
                        continue;
                    }

                    var existing = await _dbContext.StudentGuardians
                        .Where(sg => sg.StudentId == student.StudentId && sg.GuardianId == guardian.GuardianId)
                        .FirstOrDefaultAsync(cancellationToken);

                    if (existing == null)
                    {
                        var relationship = new StudentGuardian
                        {
                            StudentId = student.StudentId,
                            GuardianId = guardian.GuardianId,
                            Relationship = contact.Relationship
                        };
                        _dbContext.StudentGuardians.Add(relationship);
                        upserted++;
                    }
                    else if (existing.Relationship != contact.Relationship)
                    {
                        existing.Relationship = contact.Relationship;
                        upserted++;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to upsert student-guardian relationship for student {StudentId}, guardian {GuardianId}", wondeStudent.Id, contact.Id);
                }
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        return upserted;
    }

    private async Task<int> UpsertAttendanceMarksAsync(List<WondeAttendance> wondeAttendance, DateOnly date, CancellationToken cancellationToken)
    {
        var upserted = 0;

        foreach (var wondeAtt in wondeAttendance)
        {
            try
            {
                // Find student by external ID
                var student = await _dbContext.Students
                    .Where(s => s.ExternalStudentId == wondeAtt.StudentId)
                    .FirstOrDefaultAsync(cancellationToken);

                if (student == null)
                {
                    _logger.LogWarning("Student {StudentId} not found for attendance mark", wondeAtt.StudentId);
                    continue;
                }

                // Determine session (AM/PM) from period
                var session = DetermineSession(wondeAtt.Period);
                
                // Parse date from Wonde response
                var attendanceDate = ParseWondeDate(wondeAtt.Date, date);

                // Map Wonde status to internal status
                var status = MapWondeStatus(wondeAtt.Status);
                var reasonCode = wondeAtt.Absence?.Reason?.Code;

                var existing = await _dbContext.AttendanceMarks
                    .Where(am => am.StudentId == student.StudentId && 
                                am.Date == attendanceDate && 
                                am.Session == session)
                    .FirstOrDefaultAsync(cancellationToken);

                if (existing == null)
                {
                    existing = new AttendanceMark
                    {
                        StudentId = student.StudentId,
                        Date = attendanceDate,
                        Session = session,
                        Status = status,
                        ReasonCode = reasonCode,
                        Source = "WONDE"
                    };
                    _dbContext.AttendanceMarks.Add(existing);
                }
                else
                {
                    existing.Status = status;
                    existing.ReasonCode = reasonCode;
                    existing.Source = "WONDE";
                }

                upserted++;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to upsert attendance mark for student {StudentId}", wondeAtt.StudentId);
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        return upserted;
    }

    private static string DetermineSession(WondePeriod? period)
    {
        if (period == null)
        {
            return "AM"; // Default to AM if period is not determinable
        }

        var periodName = period.Name?.ToUpperInvariant() ?? "";
        var periodCode = period.Code?.ToUpperInvariant() ?? "";

        // Common patterns for AM/PM determination
        if (periodName.Contains("AM") || periodCode.Contains("AM") || 
            periodName.Contains("MORNING") || periodCode.StartsWith("AM"))
        {
            return "AM";
        }
        
        if (periodName.Contains("PM") || periodCode.Contains("PM") || 
            periodName.Contains("AFTERNOON") || periodCode.StartsWith("PM"))
        {
            return "PM";
        }

        // Default to AM if uncertain
        return "AM";
    }

    private static DateOnly ParseWondeDate(WondeDate? wondeDate, DateOnly fallbackDate)
    {
        if (wondeDate?.DateString != null && DateOnly.TryParse(wondeDate.DateString, out var parsedDate))
        {
            return parsedDate;
        }
        return fallbackDate;
    }

    private static string MapWondeStatus(string? wondeStatus)
    {
        return wondeStatus?.ToUpperInvariant() switch
        {
            "PRESENT" => "PRESENT",
            "ABSENT" => "ABSENT",
            "LATE" => "LATE",
            "UNKNOWN" or null => "UNKNOWN",
            _ => "UNKNOWN"
        };
    }

    private async Task PublishIngestionEventAsync(DateOnly date, int studentCount, int markCount, CancellationToken cancellationToken)
    {
        var payload = new AttendanceMarksIngestedV1(
            Date: date,
            StudentCount: studentCount,
            MarkCount: markCount,
            Source: "WONDE"
        );

        var envelope = new MessageEnvelope<AttendanceMarksIngestedV1>(
            MessageType: MessageTypes.AttendanceMarksIngestedV1,
            Version: MessageVersions.V1,
            TenantId: _tenantContext.TenantId,
            SchoolId: _tenantContext.SchoolId ?? Guid.Empty,
            CorrelationId: Guid.NewGuid().ToString(),
            OccurredAtUtc: DateTimeOffset.UtcNow,
            Payload: payload
        );

        await _messageBus.PublishAsync(envelope, cancellationToken);
    }
}

public sealed record IngestionResult
{
    public Guid SchoolId { get; set; }
    public DateOnly Date { get; set; }
    public int StudentCount { get; set; }
    public int GuardianCount { get; set; }
    public int MarkCount { get; set; }
    public DateTimeOffset StartTimeUtc { get; set; }
    public DateTimeOffset EndTimeUtc { get; set; }
    public TimeSpan Duration { get; set; }
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
}
