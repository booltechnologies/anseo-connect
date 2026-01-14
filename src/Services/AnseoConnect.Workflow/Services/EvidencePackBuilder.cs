using System.Text.Json;
using AnseoConnect.Data;
using AnseoConnect.Data.Entities;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace AnseoConnect.Workflow.Services;

/// <summary>
/// Builds comprehensive evidence packs with multiple sections and integrity hashing.
/// </summary>
public sealed class EvidencePackBuilder
{
    private readonly AnseoConnectDbContext _dbContext;
    private readonly EvidencePackIntegrityService _integrityService;
    private readonly MtssTierService? _tierService;
    private readonly ILogger<EvidencePackBuilder> _logger;

    public EvidencePackBuilder(
        AnseoConnectDbContext dbContext,
        EvidencePackIntegrityService integrityService,
        ILogger<EvidencePackBuilder> logger,
        MtssTierService? tierService = null)
    {
        _dbContext = dbContext;
        _integrityService = integrityService;
        _logger = logger;
        _tierService = tierService;
    }

    public async Task<EvidencePack> BuildAsync(EvidencePackRequest request, CancellationToken cancellationToken = default)
    {
        var caseEntity = await _dbContext.Cases
            .Include(c => c.Student)
            .FirstOrDefaultAsync(c => c.CaseId == request.CaseId, cancellationToken);

        if (caseEntity == null)
        {
            throw new InvalidOperationException($"Case {request.CaseId} not found");
        }

        // Build manifest/index as we generate sections
        var manifest = new EvidencePackManifest
        {
            GeneratedAt = DateTimeOffset.UtcNow,
            CaseId = request.CaseId,
            StudentId = caseEntity.StudentId,
            DateRange = new DateRange
            {
                Start = request.DateRangeStart,
                End = request.DateRangeEnd
            },
            Sections = new List<ManifestSection>()
        };

        // Generate PDF
        var pdfBytes = await BuildPdfAsync(caseEntity, request, manifest, cancellationToken);

        // Compute hashes
        var contentHash = _integrityService.ComputeContentHash(pdfBytes);
        var indexJson = JsonSerializer.Serialize(manifest);
        var manifestHash = _integrityService.ComputeManifestHash(indexJson);

        // Create evidence pack record
        var pack = new EvidencePack
        {
            EvidencePackId = Guid.NewGuid(),
            CaseId = request.CaseId,
            StudentId = caseEntity.StudentId,
            DateRangeStart = request.DateRangeStart,
            DateRangeEnd = request.DateRangeEnd,
            IncludedSectionsJson = JsonSerializer.Serialize(request.IncludeSections),
            Format = request.IncludeSections.HasFlag(EvidencePackSections.All) ? "PDF_WITH_ZIP" : "PDF",
            StoragePath = $"evidence/{request.CaseId}/{DateTimeOffset.UtcNow:yyyyMMddHHmmss}.pdf",
            ZipStoragePath = null, // Would be set if ZIP is generated
            IndexJson = indexJson,
            ContentHash = contentHash,
            ManifestHash = manifestHash,
            GeneratedByUserId = request.RequestedByUserId,
            GeneratedAtUtc = DateTimeOffset.UtcNow,
            GenerationPurpose = request.Purpose
        };

        _dbContext.EvidencePacks.Add(pack);
        await _dbContext.SaveChangesAsync(cancellationToken);

        // Log export
        await _integrityService.LogExportAsync(pack.EvidencePackId, request.RequestedByUserId, request.Purpose, cancellationToken);

        _logger.LogInformation("Generated evidence pack {PackId} for case {CaseId} with {SectionCount} sections",
            pack.EvidencePackId, request.CaseId, manifest.Sections.Count);

        return pack;
    }

    private async Task<byte[]> BuildPdfAsync(
        Case caseEntity,
        EvidencePackRequest request,
        EvidencePackManifest manifest,
        CancellationToken cancellationToken)
    {
        // Load all data first (QuestPDF doesn't support async in content generation)
        var attendanceData = request.IncludeSections.HasFlag(EvidencePackSections.Attendance)
            ? await LoadAttendanceDataAsync(caseEntity.StudentId, request, cancellationToken)
            : null;

        var tierHistory = request.IncludeSections.HasFlag(EvidencePackSections.TierHistory) && _tierService != null
            ? await LoadTierHistoryAsync(caseEntity.CaseId, cancellationToken)
            : null;

        var messages = request.IncludeSections.HasFlag(EvidencePackSections.Communications)
            ? await LoadMessagesAsync(caseEntity.CaseId, request, cancellationToken)
            : null;

        var letters = request.IncludeSections.HasFlag(EvidencePackSections.Letters)
            ? await LoadLettersAsync(request, cancellationToken)
            : null;

        var meetings = request.IncludeSections.HasFlag(EvidencePackSections.Meetings)
            ? await LoadMeetingsAsync(request, cancellationToken)
            : null;

        var tasks = request.IncludeSections.HasFlag(EvidencePackSections.Tasks)
            ? await LoadTasksAsync(caseEntity.CaseId, request, cancellationToken)
            : null;

        // Now generate PDF synchronously with pre-loaded data
        using var stream = new MemoryStream();

        var pageNumber = 1;

        Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(1, Unit.Inch);
                page.DefaultTextStyle(TextStyle.Default.FontSize(11));

                // Cover page
                page.Header()
                    .Column(col =>
                    {
                        col.Item().Text("Evidence Pack").FontSize(20).Bold();
                        col.Item().PaddingVertical(10);
                        col.Item().Text($"Case ID: {caseEntity.CaseId}");
                        col.Item().Text($"Student: {caseEntity.Student?.FirstName} {caseEntity.Student?.LastName}");
                        col.Item().Text($"Date Range: {request.DateRangeStart:yyyy-MM-dd} to {request.DateRangeEnd:yyyy-MM-dd}");
                        col.Item().Text($"Generated: {DateTimeOffset.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
                        col.Item().Text($"Purpose: {request.Purpose}");
                    });

                page.Content()
                    .Column(col =>
                    {
                        col.Item().PaddingBottom(10).Text("Table of Contents").FontSize(16).Bold();
                        // TOC will be populated as sections are added
                    });
            });

            pageNumber = 2;

            // Attendance Section
            if (request.IncludeSections.HasFlag(EvidencePackSections.Attendance) && attendanceData != null)
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(1, Unit.Inch);

                    page.Header().Text("Attendance").FontSize(16).Bold();

                    page.Content()
                        .Column(col =>
                        {
                            if (attendanceData.Summaries.Any())
                            {
                                col.Item().Text($"Average Attendance: {attendanceData.AvgAttendance:F1}%").FontSize(14).Bold();
                                col.Item().Text($"Total Absence Days: {attendanceData.TotalAbsences}").FontSize(14).Bold();
                                col.Item().PaddingVertical(10);

                                col.Item().Text("Daily Breakdown").FontSize(12).Bold();
                                foreach (var summary in attendanceData.Summaries.Take(50)) // Limit to first 50 days
                                {
                                    col.Item().Text($"{summary.Date:yyyy-MM-dd}: {summary.AttendancePercent:F1}% | Absences: {summary.TotalAbsenceDaysYTD}");
                                }

                                if (attendanceData.Summaries.Count > 50)
                                {
                                    col.Item().Text($"... and {attendanceData.Summaries.Count - 50} more days").FontSize(10).Italic();
                                }
                            }
                            else
                            {
                                col.Item().Text("No attendance data available for the selected date range.");
                            }
                        });
                });

                manifest.Sections.Add(new ManifestSection
                {
                    Name = "Attendance",
                    Page = pageNumber,
                    ArtifactCount = 0
                });
                pageNumber++;
            }

            // Tier History Section
            if (request.IncludeSections.HasFlag(EvidencePackSections.TierHistory) && tierHistory != null)
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(1, Unit.Inch);

                    page.Header().Text("Tier History").FontSize(16).Bold();

                    page.Content()
                        .Column(col =>
                        {
                            if (tierHistory.CurrentTier != null)
                            {
                                col.Item().Text($"Current Tier: {tierHistory.CurrentTier.Assignment.TierNumber}").FontSize(14).Bold();
                                col.Item().Text($"Rationale: {tierHistory.CurrentTier.Rationale}").FontSize(12);
                                col.Item().PaddingVertical(10);
                            }

                            if (tierHistory.History.Any())
                            {
                                col.Item().Text("Tier Changes").FontSize(12).Bold();
                                foreach (var h in tierHistory.History)
                                {
                                    col.Item().Text($"{h.ChangedAtUtc:yyyy-MM-dd}: {h.ChangeType} from Tier {h.FromTier} to Tier {h.ToTier}");
                                    col.Item().Text($"Reason: {h.ChangeReason}").FontSize(10).FontColor(Colors.Grey.Darken2);
                                }
                            }
                        });
                });

                manifest.Sections.Add(new ManifestSection
                {
                    Name = "Tier History",
                    Page = pageNumber,
                    ArtifactCount = 0
                });
                pageNumber++;
            }

            // Communications Section
            if (request.IncludeSections.HasFlag(EvidencePackSections.Communications) && messages != null)
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(1, Unit.Inch);

                    page.Header().Text("Communications").FontSize(16).Bold();

                    page.Content()
                        .Column(col =>
                        {
                            if (messages.Any())
                            {
                                col.Item().Text($"Total Messages: {messages.Count}").FontSize(14).Bold();
                                col.Item().PaddingVertical(10);

                                foreach (var msg in messages.Take(20)) // Limit to first 20 messages
                                {
                                    col.Item().Text($"{msg.CreatedAtUtc:yyyy-MM-dd HH:mm} [{msg.Channel}] {msg.Status}").FontSize(12).Bold();
                                    col.Item().Text(msg.Body ?? string.Empty).FontSize(10).FontColor(Colors.Grey.Darken2);
                                    col.Item().PaddingBottom(5);
                                }

                                if (messages.Count > 20)
                                {
                                    col.Item().Text($"... and {messages.Count - 20} more messages").FontSize(10).Italic();
                                }
                            }
                            else
                            {
                                col.Item().Text("No communications found for the selected date range.");
                            }
                        });
                });

                manifest.Sections.Add(new ManifestSection
                {
                    Name = "Communications",
                    Page = pageNumber,
                    ArtifactCount = 0
                });
                pageNumber++;
            }

            // Letters Section
            if (request.IncludeSections.HasFlag(EvidencePackSections.Letters) && letters != null)
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(1, Unit.Inch);

                    page.Header().Text("Letters").FontSize(16).Bold();

                    page.Content()
                        .Column(col =>
                        {
                            if (letters.Any())
                            {
                                col.Item().Text($"Total Letters: {letters.Count}").FontSize(14).Bold();
                                col.Item().PaddingVertical(10);

                                foreach (var letter in letters)
                                {
                                    col.Item().Text($"{letter.GeneratedAtUtc:yyyy-MM-dd}: Letter Artifact {letter.ArtifactId}").FontSize(12);
                                    col.Item().Text($"Hash: {letter.ContentHash}").FontSize(10).FontColor(Colors.Grey.Darken2);
                                }
                            }
                            else
                            {
                                col.Item().Text("No letters found for the selected date range.");
                            }
                        });
                });

                manifest.Sections.Add(new ManifestSection
                {
                    Name = "Letters",
                    Page = pageNumber,
                    ArtifactCount = 0
                });
                pageNumber++;
            }

            // Meetings Section
            if (request.IncludeSections.HasFlag(EvidencePackSections.Meetings) && meetings != null)
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(1, Unit.Inch);

                    page.Header().Text("Meetings").FontSize(16).Bold();

                    page.Content()
                        .Column(col =>
                        {
                            if (meetings.Any())
                            {
                                foreach (var meeting in meetings)
                                {
                                    col.Item().Text($"{meeting.ScheduledAtUtc:yyyy-MM-dd}: {meeting.Status}").FontSize(12).Bold();
                                    if (!string.IsNullOrEmpty(meeting.OutcomeCode))
                                    {
                                        col.Item().Text($"Outcome: {meeting.OutcomeCode}").FontSize(10);
                                    }
                                    col.Item().PaddingBottom(5);
                                }
                            }
                            else
                            {
                                col.Item().Text("No meetings found for the selected date range.");
                            }
                        });
                });

                manifest.Sections.Add(new ManifestSection
                {
                    Name = "Meetings",
                    Page = pageNumber,
                    ArtifactCount = 0
                });
                pageNumber++;
            }

            // Tasks Section
            if (request.IncludeSections.HasFlag(EvidencePackSections.Tasks) && tasks != null)
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(1, Unit.Inch);

                    page.Header().Text("Tasks").FontSize(16).Bold();

                    page.Content()
                        .Column(col =>
                        {
                            if (tasks.Any())
                            {
                                col.Item().Text($"Total Tasks: {tasks.Count}").FontSize(14).Bold();
                                col.Item().PaddingVertical(10);

                                foreach (var task in tasks)
                                {
                                    col.Item().Text($"{task.CreatedAtUtc:yyyy-MM-dd}: {task.Title} [{task.Status}]").FontSize(12);
                                    if (task.DueAtUtc.HasValue)
                                    {
                                        col.Item().Text($"Due: {task.DueAtUtc:yyyy-MM-dd}").FontSize(10).FontColor(Colors.Grey.Darken2);
                                    }
                                    col.Item().PaddingBottom(5);
                                }
                            }
                            else
                            {
                                col.Item().Text("No tasks found for the selected date range.");
                            }
                        });
                });

                manifest.Sections.Add(new ManifestSection
                {
                    Name = "Tasks",
                    Page = pageNumber,
                    ArtifactCount = 0
                });
            }
        })
        .GeneratePdf(stream);

        return stream.ToArray();
    }

    private async Task<AttendanceData?> LoadAttendanceDataAsync(Guid studentId, EvidencePackRequest request, CancellationToken cancellationToken)
    {
        var summaries = await _dbContext.AttendanceDailySummaries
            .AsNoTracking()
            .Where(s => s.StudentId == studentId &&
                       s.Date >= request.DateRangeStart &&
                       s.Date <= request.DateRangeEnd)
            .OrderBy(s => s.Date)
            .ToListAsync(cancellationToken);

        if (!summaries.Any()) return null;

        return new AttendanceData
        {
            Summaries = summaries,
            AvgAttendance = (decimal)(summaries.Average(s => (double?)s.AttendancePercent) ?? 0d),
            TotalAbsences = summaries.Sum(s => (int?)s.TotalAbsenceDaysYTD) ?? 0
        };
    }

    private async Task<TierHistoryData?> LoadTierHistoryAsync(Guid caseId, CancellationToken cancellationToken)
    {
        if (_tierService == null) return null;

        var currentTier = await _tierService.GetCurrentTierAsync(caseId, cancellationToken);
        var history = await _tierService.GetHistoryAsync(caseId, cancellationToken);

        return new TierHistoryData
        {
            CurrentTier = currentTier,
            History = history
        };
    }

    private async Task<List<Message>?> LoadMessagesAsync(Guid caseId, EvidencePackRequest request, CancellationToken cancellationToken)
    {
        var messages = await _dbContext.Messages
            .AsNoTracking()
            .Where(m => m.CaseId == caseId &&
                       m.CreatedAtUtc.Date >= request.DateRangeStart.ToDateTime(TimeOnly.MinValue) &&
                       m.CreatedAtUtc.Date <= request.DateRangeEnd.ToDateTime(TimeOnly.MinValue))
            .OrderBy(m => m.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        return messages;
    }

    private async Task<List<LetterArtifact>?> LoadLettersAsync(EvidencePackRequest request, CancellationToken cancellationToken)
    {
        var letters = await _dbContext.LetterArtifacts
            .AsNoTracking()
            .Where(l => l.InstanceId != Guid.Empty) // Would need to link to case
            .Where(l => l.GeneratedAtUtc.Date >= request.DateRangeStart.ToDateTime(TimeOnly.MinValue) &&
                       l.GeneratedAtUtc.Date <= request.DateRangeEnd.ToDateTime(TimeOnly.MinValue))
            .OrderBy(l => l.GeneratedAtUtc)
            .ToListAsync(cancellationToken);

        return letters;
    }

    private async Task<List<InterventionMeeting>?> LoadMeetingsAsync(EvidencePackRequest request, CancellationToken cancellationToken)
    {
        var meetings = await _dbContext.InterventionMeetings
            .AsNoTracking()
            .Where(m => m.InstanceId != Guid.Empty) // Would need to link to case
            .Where(m => m.ScheduledAtUtc.Date >= request.DateRangeStart.ToDateTime(TimeOnly.MinValue) &&
                       m.ScheduledAtUtc.Date <= request.DateRangeEnd.ToDateTime(TimeOnly.MinValue))
            .OrderBy(m => m.ScheduledAtUtc)
            .ToListAsync(cancellationToken);

        return meetings;
    }

    private async Task<List<WorkTask>?> LoadTasksAsync(Guid caseId, EvidencePackRequest request, CancellationToken cancellationToken)
    {
        var tasks = await _dbContext.WorkTasks
            .AsNoTracking()
            .Where(t => t.CaseId == caseId &&
                       t.CreatedAtUtc.Date >= request.DateRangeStart.ToDateTime(TimeOnly.MinValue) &&
                       t.CreatedAtUtc.Date <= request.DateRangeEnd.ToDateTime(TimeOnly.MinValue))
            .OrderBy(t => t.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        return tasks;
    }
}

internal sealed class AttendanceData
{
    public List<AttendanceDailySummary> Summaries { get; set; } = new();
    public decimal AvgAttendance { get; set; }
    public int TotalAbsences { get; set; }
}

internal sealed class TierHistoryData
{
    public TierAssignmentWithRationale? CurrentTier { get; set; }
    public List<TierAssignmentHistory> History { get; set; } = new();
}

/// <summary>
/// Request for generating an evidence pack.
/// </summary>
public sealed record EvidencePackRequest(
    Guid CaseId,
    DateOnly DateRangeStart,
    DateOnly DateRangeEnd,
    EvidencePackSections IncludeSections,
    string Purpose,
    Guid RequestedByUserId);

/// <summary>
/// Sections to include in evidence pack.
/// </summary>
[Flags]
public enum EvidencePackSections
{
    Attendance = 1,
    Communications = 2,
    Letters = 4,
    Meetings = 8,
    Tasks = 16,
    TierHistory = 32,
    Safeguarding = 64, // restricted
    All = Attendance | Communications | Letters | Meetings | Tasks | TierHistory
}

/// <summary>
/// Evidence pack manifest/index structure.
/// </summary>
public sealed class EvidencePackManifest
{
    public DateTimeOffset GeneratedAt { get; set; }
    public Guid CaseId { get; set; }
    public Guid StudentId { get; set; }
    public DateRange DateRange { get; set; } = null!;
    public List<ManifestSection> Sections { get; set; } = new();
}

/// <summary>
/// Date range in manifest.
/// </summary>
public sealed class DateRange
{
    public DateOnly Start { get; set; }
    public DateOnly End { get; set; }
}

/// <summary>
/// Section entry in manifest.
/// </summary>
public sealed class ManifestSection
{
    public string Name { get; set; } = string.Empty;
    public int Page { get; set; }
    public int ArtifactCount { get; set; }
    public List<string>? Artifacts { get; set; }
}
