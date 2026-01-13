using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AnseoConnect.Data.Migrations
{
    /// <inheritdoc />
    public partial class EpicC_HybridPlaybooks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AutomationMetrics",
                columns: table => new
                {
                    MetricsId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    PlaybooksStarted = table.Column<int>(type: "int", nullable: false),
                    StepsScheduled = table.Column<int>(type: "int", nullable: false),
                    StepsSent = table.Column<int>(type: "int", nullable: false),
                    PlaybooksStoppedByReply = table.Column<int>(type: "int", nullable: false),
                    PlaybooksStoppedByImprovement = table.Column<int>(type: "int", nullable: false),
                    Escalations = table.Column<int>(type: "int", nullable: false),
                    EstimatedMinutesSaved = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    AttendanceImprovementDelta = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SchoolId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AutomationMetrics", x => x.MetricsId);
                });

            migrationBuilder.CreateTable(
                name: "JobLocks",
                columns: table => new
                {
                    LockName = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    HolderInstanceId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    AcquiredAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ExpiresAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JobLocks", x => x.LockName);
                });

            migrationBuilder.CreateTable(
                name: "PlaybookDefinitions",
                columns: table => new
                {
                    PlaybookId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SchoolId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TriggerStageType = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    StopConditionsJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    EscalationConditionsJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    EscalationAfterDays = table.Column<int>(type: "int", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlaybookDefinitions", x => x.PlaybookId);
                });

            migrationBuilder.CreateTable(
                name: "PlaybookExecutionLogs",
                columns: table => new
                {
                    LogId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RunId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StepId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Channel = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    OutboxMessageId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IdempotencyKey = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    SkipReason = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ScheduledForUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ExecutedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SchoolId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlaybookExecutionLogs", x => x.LogId);
                });

            migrationBuilder.CreateTable(
                name: "PlaybookRuns",
                columns: table => new
                {
                    RunId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PlaybookId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    InstanceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StudentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    GuardianId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    TriggeredAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    StoppedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    StopReason = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CurrentStepOrder = table.Column<int>(type: "int", nullable: false),
                    NextStepScheduledAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SchoolId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlaybookRuns", x => x.RunId);
                });

            migrationBuilder.CreateTable(
                name: "PlaybookSteps",
                columns: table => new
                {
                    StepId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PlaybookId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Order = table.Column<int>(type: "int", nullable: false),
                    OffsetDays = table.Column<int>(type: "int", nullable: false),
                    Channel = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    TemplateKey = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    FallbackChannel = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    SkipIfPreviousReplied = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlaybookSteps", x => x.StepId);
                });

            migrationBuilder.CreateTable(
                name: "TelemetryEvents",
                columns: table => new
                {
                    TelemetryEventId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EventType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PlaybookRunId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    StudentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    MetadataJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OccurredAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SchoolId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TelemetryEvents", x => x.TelemetryEventId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AutomationMetrics_Date",
                table: "AutomationMetrics",
                columns: new[] { "TenantId", "SchoolId", "Date" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PlaybookDefinitions_TriggerStage",
                table: "PlaybookDefinitions",
                columns: new[] { "TenantId", "TriggerStageType", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_PlaybookExecutionLogs_Idempotency",
                table: "PlaybookExecutionLogs",
                columns: new[] { "TenantId", "IdempotencyKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PlaybookExecutionLogs_Run",
                table: "PlaybookExecutionLogs",
                columns: new[] { "RunId", "ScheduledForUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_PlaybookRuns_Instance",
                table: "PlaybookRuns",
                column: "InstanceId");

            migrationBuilder.CreateIndex(
                name: "IX_PlaybookRuns_Status_NextStep",
                table: "PlaybookRuns",
                columns: new[] { "TenantId", "SchoolId", "Status", "NextStepScheduledAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_PlaybookSteps_Playbook_Order",
                table: "PlaybookSteps",
                columns: new[] { "PlaybookId", "Order" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TelemetryEvents_Date",
                table: "TelemetryEvents",
                columns: new[] { "TenantId", "SchoolId", "OccurredAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AutomationMetrics");

            migrationBuilder.DropTable(
                name: "JobLocks");

            migrationBuilder.DropTable(
                name: "PlaybookDefinitions");

            migrationBuilder.DropTable(
                name: "PlaybookExecutionLogs");

            migrationBuilder.DropTable(
                name: "PlaybookRuns");

            migrationBuilder.DropTable(
                name: "PlaybookSteps");

            migrationBuilder.DropTable(
                name: "TelemetryEvents");
        }
    }
}
