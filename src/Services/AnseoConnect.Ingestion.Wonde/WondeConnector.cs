using AnseoConnect.Contracts.SIS;
using AnseoConnect.Data;
using AnseoConnect.Data.Entities;
using AnseoConnect.Data.MultiTenancy;
using AnseoConnect.Ingestion.Wonde.Client;
using AnseoConnect.Shared;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace AnseoConnect.Ingestion.Wonde;

/// <summary>
/// Wonde SIS connector implementation.
/// </summary>
public sealed class WondeConnector : ISisConnector
{
    private static readonly HashSet<SisCapability> _capabilities = new()
    {
        SisCapability.RosterSync,
        SisCapability.ContactsSync,
        SisCapability.AttendanceSync,
        SisCapability.ClassesSync,
        SisCapability.TimetableSync
    };

    private readonly IWondeClient _wondeClient;
    private readonly AnseoConnectDbContext _dbContext;
    private readonly IMessageBus _messageBus;
    private readonly ILogger<WondeConnector> _logger;
    private readonly ITenantContext _tenantContext;
    private readonly Services.IReasonCodeMapper? _reasonCodeMapper;

    public string ProviderId => "WONDE";
    public IReadOnlySet<SisCapability> Capabilities => _capabilities;

    public WondeConnector(
        IWondeClient wondeClient,
        AnseoConnectDbContext dbContext,
        IMessageBus messageBus,
        ILogger<WondeConnector> logger,
        ITenantContext tenantContext,
        Services.IReasonCodeMapper? reasonCodeMapper = null)
    {
        _wondeClient = wondeClient;
        _dbContext = dbContext;
        _messageBus = messageBus;
        _logger = logger;
        _tenantContext = tenantContext;
        _reasonCodeMapper = reasonCodeMapper;
    }

    public async Task<SyncRunResult> SyncRosterAsync(Guid schoolId, SyncOptions options, CancellationToken cancellationToken = default)
    {
        var startTime = DateTimeOffset.UtcNow;
        var school = await GetSchoolAsync(schoolId, cancellationToken);
        var syncRun = await CreateSyncRunAsync(school, "Roster", options, null, cancellationToken);

        var result = new SyncRunResult
        {
            SyncRunId = syncRun.SyncRunId,
            StartTimeUtc = startTime
        };

        try
        {
            var updatedAfter = options.ForceFullSync ? null : GetSyncWatermark(school.TenantId, school.SchoolId, "Student", options.UpdatedAfter);
            var studentsResponse = await _wondeClient.GetStudentsAsync(school.WondeSchoolId!, updatedAfter, cancellationToken);

            if (studentsResponse?.Data != null)
            {
                var upsertResult = await UpsertStudentsAsync(studentsResponse.Data, syncRun, options, cancellationToken);
                result.InsertedCount = upsertResult.Inserted;
                result.UpdatedCount = upsertResult.Updated;
                result.SkippedCount = upsertResult.Skipped;
                result.ErrorCount = upsertResult.Errors;

                if (options.StoreMetrics)
                {
                    await CreateSyncMetricAsync(syncRun.SyncRunId, "Student", upsertResult, cancellationToken);
                }
            }

            result.Success = true;
            await CompleteSyncRunAsync(syncRun, result, school, cancellationToken);
            await UpdateSyncStateAsync(school, "Student", startTime, cancellationToken);

            _logger.LogInformation(
                "Roster sync completed for school {SchoolId}. Inserted: {Inserted}, Updated: {Updated}, Errors: {Errors}",
                schoolId, result.InsertedCount, result.UpdatedCount, result.ErrorCount);

            return result;
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.ErrorMessage = ex.Message;
            await FailSyncRunAsync(syncRun, result, ex, school, cancellationToken);
            _logger.LogError(ex, "Roster sync failed for school {SchoolId}", schoolId);
            return result;
        }
        finally
        {
            result.EndTimeUtc = DateTimeOffset.UtcNow;
        }
    }

    public async Task<SyncRunResult> SyncContactsAsync(Guid schoolId, SyncOptions options, CancellationToken cancellationToken = default)
    {
        var startTime = DateTimeOffset.UtcNow;
        var school = await GetSchoolAsync(schoolId, cancellationToken);
        var syncRun = await CreateSyncRunAsync(school, "Contacts", options, null, cancellationToken);

        var result = new SyncRunResult
        {
            SyncRunId = syncRun.SyncRunId,
            StartTimeUtc = startTime
        };

        try
        {
            var updatedAfter = options.ForceFullSync ? null : GetSyncWatermark(school.TenantId, school.SchoolId, "Guardian", options.UpdatedAfter);
            var contactsResponse = await _wondeClient.GetContactsAsync(school.WondeSchoolId!, updatedAfter, cancellationToken);

            if (contactsResponse?.Data != null)
            {
                var upsertResult = await UpsertGuardiansAsync(contactsResponse.Data, syncRun, options, cancellationToken);
                result.InsertedCount = upsertResult.Inserted;
                result.UpdatedCount = upsertResult.Updated;
                result.SkippedCount = upsertResult.Skipped;
                result.ErrorCount = upsertResult.Errors;

                if (options.StoreMetrics)
                {
                    await CreateSyncMetricAsync(syncRun.SyncRunId, "Guardian", upsertResult, cancellationToken);
                }
            }

            result.Success = true;
            await CompleteSyncRunAsync(syncRun, result, school, cancellationToken);
            await UpdateSyncStateAsync(school, "Guardian", startTime, cancellationToken);

            _logger.LogInformation(
                "Contacts sync completed for school {SchoolId}. Inserted: {Inserted}, Updated: {Updated}, Errors: {Errors}",
                schoolId, result.InsertedCount, result.UpdatedCount, result.ErrorCount);

            return result;
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.ErrorMessage = ex.Message;
            await FailSyncRunAsync(syncRun, result, ex, school, cancellationToken);
            _logger.LogError(ex, "Contacts sync failed for school {SchoolId}", schoolId);
            return result;
        }
        finally
        {
            result.EndTimeUtc = DateTimeOffset.UtcNow;
        }
    }

    public async Task<SyncRunResult> SyncAttendanceAsync(Guid schoolId, DateOnly date, SyncOptions options, CancellationToken cancellationToken = default)
    {
        var startTime = DateTimeOffset.UtcNow;
        var school = await GetSchoolAsync(schoolId, cancellationToken);
        var syncRun = await CreateSyncRunAsync(school, "Attendance", options, date, cancellationToken);

        var result = new SyncRunResult
        {
            SyncRunId = syncRun.SyncRunId,
            StartTimeUtc = startTime
        };

        try
        {
            var attendanceResponse = await _wondeClient.GetAttendanceAsync(school.WondeSchoolId!, date, cancellationToken);

            if (attendanceResponse?.Data != null)
            {
                var upsertResult = await UpsertAttendanceMarksAsync(attendanceResponse.Data, date, syncRun, options, cancellationToken);
                result.InsertedCount = upsertResult.Inserted;
                result.UpdatedCount = upsertResult.Updated;
                result.SkippedCount = upsertResult.Skipped;
                result.ErrorCount = upsertResult.Errors;

                if (options.StoreMetrics)
                {
                    await CreateSyncMetricAsync(syncRun.SyncRunId, "AttendanceMark", upsertResult, cancellationToken);
                }
            }

            result.Success = true;
            await CompleteSyncRunAsync(syncRun, result, school, cancellationToken);
            await UpdateSyncStateAsync(school, "Attendance", startTime, cancellationToken);

            // Publish event if marks were ingested
            if (result.InsertedCount + result.UpdatedCount > 0)
            {
                await PublishIngestionEventAsync(date, result.InsertedCount + result.UpdatedCount, cancellationToken);
            }

            _logger.LogInformation(
                "Attendance sync completed for school {SchoolId}, date {Date}. Inserted: {Inserted}, Updated: {Updated}, Errors: {Errors}",
                schoolId, date, result.InsertedCount, result.UpdatedCount, result.ErrorCount);

            return result;
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.ErrorMessage = ex.Message;
            await FailSyncRunAsync(syncRun, result, ex, school, cancellationToken);
            _logger.LogError(ex, "Attendance sync failed for school {SchoolId}, date {Date}", schoolId, date);
            return result;
        }
        finally
        {
            result.EndTimeUtc = DateTimeOffset.UtcNow;
        }
    }

    public async Task<SyncRunResult> SyncClassesAsync(Guid schoolId, SyncOptions options, CancellationToken cancellationToken = default)
    {
        var startTime = DateTimeOffset.UtcNow;
        var school = await GetSchoolAsync(schoolId, cancellationToken);
        var syncRun = await CreateSyncRunAsync(school, "Classes", options, null, cancellationToken);

        var result = new SyncRunResult
        {
            SyncRunId = syncRun.SyncRunId,
            StartTimeUtc = startTime
        };

        try
        {
            var updatedAfter = options.ForceFullSync ? null : GetSyncWatermark(school.TenantId, school.SchoolId, "Class", options.UpdatedAfter);
            var classesResponse = await _wondeClient.GetClassesAsync(school.WondeSchoolId!, updatedAfter, cancellationToken);

            if (classesResponse?.Data != null)
            {
                var upsertResult = await UpsertClassesAsync(classesResponse.Data, syncRun, options, cancellationToken);
                result.InsertedCount = upsertResult.Inserted;
                result.UpdatedCount = upsertResult.Updated;
                result.SkippedCount = upsertResult.Skipped;
                result.ErrorCount = upsertResult.Errors;

                if (options.StoreMetrics)
                {
                    await CreateSyncMetricAsync(syncRun.SyncRunId, "ClassGroup", upsertResult, cancellationToken);
                }
            }

            result.Success = true;
            await CompleteSyncRunAsync(syncRun, result, school, cancellationToken);
            await UpdateSyncStateAsync(school, "Class", startTime, cancellationToken);

            _logger.LogInformation(
                "Classes sync completed for school {SchoolId}. Inserted: {Inserted}, Updated: {Updated}, Errors: {Errors}",
                schoolId, result.InsertedCount, result.UpdatedCount, result.ErrorCount);

            return result;
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.ErrorMessage = ex.Message;
            await FailSyncRunAsync(syncRun, result, ex, school, cancellationToken);
            _logger.LogError(ex, "Classes sync failed for school {SchoolId}", schoolId);
            return result;
        }
        finally
        {
            result.EndTimeUtc = DateTimeOffset.UtcNow;
        }
    }

    public async Task<SyncRunResult> SyncTimetableAsync(Guid schoolId, SyncOptions options, CancellationToken cancellationToken = default)
    {
        var startTime = DateTimeOffset.UtcNow;
        var school = await GetSchoolAsync(schoolId, cancellationToken);
        var syncRun = await CreateSyncRunAsync(school, "Timetable", options, null, cancellationToken);

        var result = new SyncRunResult
        {
            SyncRunId = syncRun.SyncRunId,
            StartTimeUtc = startTime
        };

        try
        {
            var timetableResponse = await _wondeClient.GetTimetableAsync(school.WondeSchoolId!, cancellationToken);

            if (timetableResponse?.Data != null)
            {
                // Timetable sync is currently a stub - can be enhanced later to store timetable periods
                result.Success = true;
                result.Notes = $"Retrieved {timetableResponse.Data.Count} timetable periods (storage not yet implemented)";
            }
            else
            {
                result.Success = true;
            }

            await CompleteSyncRunAsync(syncRun, result, school, cancellationToken);
            await UpdateSyncStateAsync(school, "Timetable", startTime, cancellationToken);

            _logger.LogInformation("Timetable sync completed for school {SchoolId}", schoolId);

            return result;
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.ErrorMessage = ex.Message;
            await FailSyncRunAsync(syncRun, result, ex, school, cancellationToken);
            _logger.LogError(ex, "Timetable sync failed for school {SchoolId}", schoolId);
            return result;
        }
        finally
        {
            result.EndTimeUtc = DateTimeOffset.UtcNow;
        }
    }

    #region Helper Methods

    private async Task<School> GetSchoolAsync(Guid schoolId, CancellationToken cancellationToken)
    {
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

        return school;
    }

    private async Task<SyncRun> CreateSyncRunAsync(School school, string syncType, SyncOptions options, DateOnly? attendanceDate, CancellationToken cancellationToken)
    {
        var syncRun = new SyncRun
        {
            TenantId = school.TenantId,
            SchoolId = school.SchoolId,
            ProviderId = ProviderId,
            SyncType = syncType,
            StartedAtUtc = DateTimeOffset.UtcNow,
            Status = "RUNNING",
            WasFullSync = options.ForceFullSync,
            SyncWatermark = options.UpdatedAfter,
            AttendanceDate = attendanceDate
        };

        _dbContext.SyncRuns.Add(syncRun);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return syncRun;
    }

    private async Task CompleteSyncRunAsync(SyncRun syncRun, SyncRunResult result, School school, CancellationToken cancellationToken)
    {
        syncRun.CompletedAtUtc = result.EndTimeUtc;
        syncRun.Status = "SUCCEEDED";
        syncRun.Notes = $"Inserted:{result.InsertedCount} Updated:{result.UpdatedCount} Skipped:{result.SkippedCount} Errors:{result.ErrorCount}";

        school.SyncStatus = SyncStatus.Healthy;
        school.SyncErrorCount = 0;
        school.LastSyncUtc = DateTimeOffset.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task FailSyncRunAsync(SyncRun syncRun, SyncRunResult result, Exception ex, School school, CancellationToken cancellationToken)
    {
        syncRun.CompletedAtUtc = result.EndTimeUtc;
        syncRun.Status = "FAILED";
        syncRun.Notes = result.ErrorMessage;

        school.SyncStatus = SyncStatus.Failed;
        school.SyncErrorCount += 1;

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task<SyncMetric> CreateSyncMetricAsync(Guid syncRunId, string entityType, UpsertResult upsertResult, CancellationToken cancellationToken)
    {
        var metric = new SyncMetric
        {
            SyncRunId = syncRunId,
            EntityType = entityType,
            InsertedCount = upsertResult.Inserted,
            UpdatedCount = upsertResult.Updated,
            SkippedCount = upsertResult.Skipped,
            ErrorCount = upsertResult.Errors
        };

        _dbContext.SyncMetrics.Add(metric);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return metric;
    }

    private DateTimeOffset? GetSyncWatermark(Guid tenantId, Guid schoolId, string entityType, DateTimeOffset? providedWatermark)
    {
        if (providedWatermark.HasValue)
        {
            return providedWatermark;
        }

        // Try to get from SchoolSyncState
        var syncState = _dbContext.SchoolSyncStates
            .AsNoTracking()
            .Where(s => s.TenantId == tenantId && s.SchoolId == schoolId && s.ProviderId == ProviderId && s.EntityType == entityType)
            .FirstOrDefault();

        return syncState?.LastSyncWatermarkUtc;
    }

    private async Task UpdateSyncStateAsync(School school, string entityType, DateTimeOffset syncTime, CancellationToken cancellationToken)
    {
        var syncState = await _dbContext.SchoolSyncStates
            .Where(s => s.TenantId == school.TenantId && s.SchoolId == school.SchoolId && s.ProviderId == ProviderId && s.EntityType == entityType)
            .FirstOrDefaultAsync(cancellationToken);

        if (syncState == null)
        {
            syncState = new SchoolSyncState
            {
                TenantId = school.TenantId,
                SchoolId = school.SchoolId,
                ProviderId = ProviderId,
                EntityType = entityType
            };
            _dbContext.SchoolSyncStates.Add(syncState);
        }

        syncState.LastSyncWatermarkUtc = syncTime;
        syncState.LastSuccessfulSyncUtc = syncTime;
        syncState.ConsecutiveFailures = 0;
        syncState.LastError = null;

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private sealed record UpsertResult(int Inserted, int Updated, int Skipped, int Errors);

    private async Task<UpsertResult> UpsertStudentsAsync(
        List<Client.WondeStudent> wondeStudents,
        SyncRun syncRun,
        SyncOptions options,
        CancellationToken cancellationToken)
    {
        var inserted = 0;
        var updated = 0;
        var skipped = 0;
        var errors = 0;

        foreach (var wondeStudent in wondeStudents)
        {
            try
            {
                var existing = await _dbContext.Students
                    .Where(s => s.ExternalStudentId == wondeStudent.Id)
                    .FirstOrDefaultAsync(cancellationToken);

                var isNew = existing == null;

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

                if (options.ArchivePayloads)
                {
                    await ArchivePayloadAsync(syncRun.SyncRunId, "Student", wondeStudent.Id, wondeStudent, cancellationToken);
                }

                if (isNew) inserted++;
                else updated++;

                await _dbContext.SaveChangesAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                errors++;
                await RecordSyncErrorAsync(syncRun.SyncRunId, "Student", wondeStudent.Id, ex, wondeStudent, cancellationToken);
                _logger.LogError(ex, "Failed to upsert student {StudentId}", wondeStudent.Id);
            }
        }

        return new UpsertResult(inserted, updated, skipped, errors);
    }

    private async Task<UpsertResult> UpsertGuardiansAsync(
        List<Client.WondeContact> wondeContacts,
        SyncRun syncRun,
        SyncOptions options,
        CancellationToken cancellationToken)
    {
        var inserted = 0;
        var updated = 0;
        var skipped = 0;
        var errors = 0;

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

                var isNew = existing == null;

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

                if (options.ArchivePayloads)
                {
                    await ArchivePayloadAsync(syncRun.SyncRunId, "Guardian", wondeContact.Id, wondeContact, cancellationToken);
                }

                if (isNew) inserted++;
                else updated++;

                await _dbContext.SaveChangesAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                errors++;
                await RecordSyncErrorAsync(syncRun.SyncRunId, "Guardian", wondeContact.Id, ex, wondeContact, cancellationToken);
                _logger.LogError(ex, "Failed to upsert guardian {GuardianId}", wondeContact.Id);
            }
        }

        return new UpsertResult(inserted, updated, skipped, errors);
    }

    private async Task<UpsertResult> UpsertAttendanceMarksAsync(
        List<Client.WondeAttendance> wondeAttendance,
        DateOnly date,
        SyncRun syncRun,
        SyncOptions options,
        CancellationToken cancellationToken)
    {
        var inserted = 0;
        var updated = 0;
        var skipped = 0;
        var errors = 0;

        foreach (var wondeAtt in wondeAttendance)
        {
            try
            {
                var student = await _dbContext.Students
                    .Where(s => s.ExternalStudentId == wondeAtt.StudentId)
                    .FirstOrDefaultAsync(cancellationToken);

                if (student == null)
                {
                    skipped++;
                    _logger.LogWarning("Student {StudentId} not found for attendance mark", wondeAtt.StudentId);
                    continue;
                }

                var session = DetermineSession(wondeAtt.Period);
                var attendanceDate = ParseWondeDate(wondeAtt.Date, date);
                var status = MapWondeStatus(wondeAtt.Status);
                
                // Map reason code using ReasonCodeMapper if available
                var providerReasonCode = wondeAtt.Absence?.Reason?.Code;
                string? reasonCode = providerReasonCode;
                if (_reasonCodeMapper != null && !string.IsNullOrWhiteSpace(providerReasonCode))
                {
                    reasonCode = await _reasonCodeMapper.MapToInternalAsync(
                        ProviderId,
                        providerReasonCode,
                        school.TenantId,
                        school.SchoolId,
                        cancellationToken);
                }

                var existing = await _dbContext.AttendanceMarks
                    .Where(am => am.StudentId == student.StudentId &&
                                am.Date == attendanceDate &&
                                am.Session == session)
                    .FirstOrDefaultAsync(cancellationToken);

                var isNew = existing == null;

                if (existing == null)
                {
                    existing = new AttendanceMark
                    {
                        StudentId = student.StudentId,
                        Date = attendanceDate,
                        Session = session,
                        Status = status,
                        ReasonCode = reasonCode,
                        Source = "WONDE",
                        RawPayloadJson = options.ArchivePayloads ? JsonSerializer.Serialize(wondeAtt) : null
                    };
                    _dbContext.AttendanceMarks.Add(existing);
                }
                else
                {
                    existing.Status = status;
                    existing.ReasonCode = reasonCode;
                    existing.Source = "WONDE";
                    existing.RawPayloadJson = options.ArchivePayloads ? JsonSerializer.Serialize(wondeAtt) : existing.RawPayloadJson;
                }

                if (options.ArchivePayloads && !options.StoreMetrics) // Only archive if not already in RawPayloadJson
                {
                    await ArchivePayloadAsync(syncRun.SyncRunId, "Attendance", wondeAtt.StudentId, wondeAtt, cancellationToken);
                }

                if (isNew) inserted++;
                else updated++;

                await _dbContext.SaveChangesAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                errors++;
                await RecordSyncErrorAsync(syncRun.SyncRunId, "Attendance", wondeAtt.StudentId, ex, wondeAtt, cancellationToken);
                _logger.LogError(ex, "Failed to upsert attendance mark for student {StudentId}", wondeAtt.StudentId);
            }
        }

        return new UpsertResult(inserted, updated, skipped, errors);
    }

    private async Task ArchivePayloadAsync<T>(Guid syncRunId, string entityType, string externalId, T payload, CancellationToken cancellationToken)
    {
        var archive = new SyncPayloadArchive
        {
            SyncRunId = syncRunId,
            EntityType = entityType,
            ExternalId = externalId,
            PayloadJson = JsonSerializer.Serialize(payload),
            CapturedAtUtc = DateTimeOffset.UtcNow,
            ExpiresAtUtc = DateTimeOffset.UtcNow.AddYears(7) // Default 7-year retention for GDPR
        };

        _dbContext.SyncPayloadArchives.Add(archive);
        // Note: SaveChangesAsync is called by the caller after the loop
    }

    private async Task RecordSyncErrorAsync(Guid syncRunId, string entityType, string externalId, Exception ex, object? payload, CancellationToken cancellationToken)
    {
        var error = new SyncError
        {
            SyncRunId = syncRunId,
            EntityType = entityType,
            ExternalId = externalId,
            ErrorMessage = ex.Message,
            RawPayloadJson = payload != null ? JsonSerializer.Serialize(payload) : null,
            OccurredAtUtc = DateTimeOffset.UtcNow
        };

        _dbContext.SyncErrors.Add(error);
        // Note: SaveChangesAsync is called by the caller after handling
    }

    private static string DetermineSession(Client.WondePeriod? period)
    {
        if (period == null) return "AM";

        var periodName = period.Name?.ToUpperInvariant() ?? "";
        var periodCode = period.Code?.ToUpperInvariant() ?? "";

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

        return "AM";
    }

    private static DateOnly ParseWondeDate(Client.WondeDate? wondeDate, DateOnly fallbackDate)
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

    private async Task<UpsertResult> UpsertClassesAsync(
        List<Client.WondeClass> wondeClasses,
        SyncRun syncRun,
        SyncOptions options,
        CancellationToken cancellationToken)
    {
        var inserted = 0;
        var updated = 0;
        var skipped = 0;
        var errors = 0;

        foreach (var wondeClass in wondeClasses)
        {
            try
            {
                var existing = await _dbContext.ClassGroups
                    .Where(c => c.ExternalClassId == wondeClass.Id)
                    .FirstOrDefaultAsync(cancellationToken);

                var isNew = existing == null;

                if (existing == null)
                {
                    existing = new ClassGroup
                    {
                        ExternalClassId = wondeClass.Id,
                        Name = wondeClass.Name ?? "",
                        Code = wondeClass.Code,
                        AcademicYear = wondeClass.AcademicYear?.Name,
                        IsActive = wondeClass.Active ?? true,
                        Source = "WONDE",
                        LastSyncedUtc = DateTimeOffset.UtcNow
                    };
                    _dbContext.ClassGroups.Add(existing);
                }
                else
                {
                    existing.Name = wondeClass.Name ?? existing.Name;
                    existing.Code = wondeClass.Code ?? existing.Code;
                    existing.AcademicYear = wondeClass.AcademicYear?.Name ?? existing.AcademicYear;
                    existing.IsActive = wondeClass.Active ?? existing.IsActive;
                    existing.LastSyncedUtc = DateTimeOffset.UtcNow;
                }

                // Sync student enrollments
                if (wondeClass.Students != null && wondeClass.Students.Any())
                {
                    await UpsertStudentClassEnrollmentsAsync(existing.ClassGroupId, wondeClass.Students.Select(s => s.Id).ToList(), cancellationToken);
                }

                if (options.ArchivePayloads)
                {
                    await ArchivePayloadAsync(syncRun.SyncRunId, "Class", wondeClass.Id, wondeClass, cancellationToken);
                }

                if (isNew) inserted++;
                else updated++;

                await _dbContext.SaveChangesAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                errors++;
                await RecordSyncErrorAsync(syncRun.SyncRunId, "Class", wondeClass.Id, ex, wondeClass, cancellationToken);
                _logger.LogError(ex, "Failed to upsert class {ClassId}", wondeClass.Id);
            }
        }

        return new UpsertResult(inserted, updated, skipped, errors);
    }

    private async Task UpsertStudentClassEnrollmentsAsync(Guid classGroupId, List<string> studentExternalIds, CancellationToken cancellationToken)
    {
        // Find all students by external IDs
        var students = await _dbContext.Students
            .Where(s => studentExternalIds.Contains(s.ExternalStudentId))
            .ToListAsync(cancellationToken);

        var studentIds = students.Select(s => s.StudentId).ToList();

        // Get existing enrollments
        var existingEnrollments = await _dbContext.StudentClassEnrollments
            .Where(e => e.ClassGroupId == classGroupId && studentIds.Contains(e.StudentId))
            .ToListAsync(cancellationToken);

        var existingStudentIds = existingEnrollments.Select(e => e.StudentId).ToHashSet();

        // Add new enrollments
        foreach (var student in students)
        {
            if (!existingStudentIds.Contains(student.StudentId))
            {
                var enrollment = new StudentClassEnrollment
                {
                    StudentId = student.StudentId,
                    ClassGroupId = classGroupId,
                    IsActive = true,
                    StartDate = DateOnly.FromDateTime(DateTime.UtcNow),
                    LastSyncedUtc = DateTimeOffset.UtcNow
                };
                _dbContext.StudentClassEnrollments.Add(enrollment);
            }
            else
            {
                var enrollment = existingEnrollments.First(e => e.StudentId == student.StudentId);
                enrollment.IsActive = true;
                enrollment.LastSyncedUtc = DateTimeOffset.UtcNow;
            }
        }

        // Deactivate enrollments that are no longer in the class
        var allEnrollments = await _dbContext.StudentClassEnrollments
            .Where(e => e.ClassGroupId == classGroupId)
            .ToListAsync(cancellationToken);

        foreach (var enrollment in allEnrollments)
        {
            if (!studentIds.Contains(enrollment.StudentId) && enrollment.IsActive)
            {
                enrollment.IsActive = false;
                enrollment.EndDate = DateOnly.FromDateTime(DateTime.UtcNow);
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task PublishIngestionEventAsync(DateOnly date, int markCount, CancellationToken cancellationToken)
    {
        var payload = new AnseoConnect.Contracts.Events.AttendanceMarksIngestedV1(
            Date: date,
            StudentCount: 0, // Will be updated when we track student count
            MarkCount: markCount,
            Source: "WONDE"
        );

        var envelope = new AnseoConnect.Contracts.Common.MessageEnvelope<AnseoConnect.Contracts.Events.AttendanceMarksIngestedV1>(
            MessageType: AnseoConnect.Contracts.Common.MessageTypes.AttendanceMarksIngestedV1,
            Version: AnseoConnect.Contracts.Common.MessageVersions.V1,
            TenantId: _tenantContext.TenantId,
            SchoolId: _tenantContext.SchoolId ?? Guid.Empty,
            CorrelationId: Guid.NewGuid().ToString(),
            OccurredAtUtc: DateTimeOffset.UtcNow,
            Payload: payload
        );

        await _messageBus.PublishAsync(envelope, cancellationToken);
    }

    #endregion
}
