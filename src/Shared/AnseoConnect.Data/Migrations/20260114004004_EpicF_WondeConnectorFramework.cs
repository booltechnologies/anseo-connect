using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AnseoConnect.Data.Migrations
{
    /// <inheritdoc />
    public partial class EpicF_WondeConnectorFramework : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "ErrorRateThreshold",
                table: "IngestionSyncLogs",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MismatchDetailsJson",
                table: "IngestionSyncLogs",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MismatchThreshold",
                table: "IngestionSyncLogs",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ClassGroups",
                columns: table => new
                {
                    ClassGroupId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ExternalClassId = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Code = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    AcademicYear = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    LastSyncedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Source = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SchoolId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClassGroups", x => x.ClassGroupId);
                });

            migrationBuilder.CreateTable(
                name: "ReasonCodeMappings",
                columns: table => new
                {
                    ReasonCodeMappingId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProviderId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ProviderCode = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    ProviderDescription = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    InternalCode = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SchoolId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReasonCodeMappings", x => x.ReasonCodeMappingId);
                });

            migrationBuilder.CreateTable(
                name: "SchoolSyncStates",
                columns: table => new
                {
                    SchoolSyncStateId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProviderId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    EntityType = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    LastSyncWatermarkUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LastSuccessfulSyncUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ConsecutiveFailures = table.Column<int>(type: "int", nullable: false),
                    LastError = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: true),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SchoolId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SchoolSyncStates", x => x.SchoolSyncStateId);
                });

            migrationBuilder.CreateTable(
                name: "SyncPayloadArchives",
                columns: table => new
                {
                    ArchiveId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SyncRunId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EntityType = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ExternalId = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    PayloadJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CapturedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ExpiresAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SchoolId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SyncPayloadArchives", x => x.ArchiveId);
                });

            migrationBuilder.CreateTable(
                name: "SyncRuns",
                columns: table => new
                {
                    SyncRunId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProviderId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    SyncType = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    StartedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CompletedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    WasFullSync = table.Column<bool>(type: "bit", nullable: false),
                    SyncWatermark = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    AttendanceDate = table.Column<DateOnly>(type: "date", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SchoolId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SyncRuns", x => x.SyncRunId);
                });

            migrationBuilder.CreateTable(
                name: "StudentClassEnrollments",
                columns: table => new
                {
                    EnrollmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StudentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ClassGroupId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StartDate = table.Column<DateOnly>(type: "date", nullable: true),
                    EndDate = table.Column<DateOnly>(type: "date", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    LastSyncedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SchoolId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StudentClassEnrollments", x => x.EnrollmentId);
                    table.ForeignKey(
                        name: "FK_StudentClassEnrollments_ClassGroups_ClassGroupId",
                        column: x => x.ClassGroupId,
                        principalTable: "ClassGroups",
                        principalColumn: "ClassGroupId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_StudentClassEnrollments_Students_StudentId",
                        column: x => x.StudentId,
                        principalTable: "Students",
                        principalColumn: "StudentId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SyncErrors",
                columns: table => new
                {
                    SyncErrorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SyncRunId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EntityType = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ExternalId = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ErrorMessage = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RawPayloadJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OccurredAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SchoolId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SyncErrors", x => x.SyncErrorId);
                    table.ForeignKey(
                        name: "FK_SyncErrors_SyncRuns_SyncRunId",
                        column: x => x.SyncRunId,
                        principalTable: "SyncRuns",
                        principalColumn: "SyncRunId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SyncMetrics",
                columns: table => new
                {
                    SyncMetricId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SyncRunId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EntityType = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    InsertedCount = table.Column<int>(type: "int", nullable: false),
                    UpdatedCount = table.Column<int>(type: "int", nullable: false),
                    SkippedCount = table.Column<int>(type: "int", nullable: false),
                    ErrorCount = table.Column<int>(type: "int", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SchoolId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SyncMetrics", x => x.SyncMetricId);
                    table.ForeignKey(
                        name: "FK_SyncMetrics_SyncRuns_SyncRunId",
                        column: x => x.SyncRunId,
                        principalTable: "SyncRuns",
                        principalColumn: "SyncRunId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ClassGroups_ExternalId",
                table: "ClassGroups",
                columns: new[] { "TenantId", "SchoolId", "ExternalClassId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ReasonCodeMappings_Provider_Code",
                table: "ReasonCodeMappings",
                columns: new[] { "TenantId", "SchoolId", "ProviderId", "ProviderCode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SchoolSyncStates_Provider_Entity",
                table: "SchoolSyncStates",
                columns: new[] { "TenantId", "SchoolId", "ProviderId", "EntityType" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StudentClassEnrollments_ClassGroup",
                table: "StudentClassEnrollments",
                column: "ClassGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_StudentClassEnrollments_Student",
                table: "StudentClassEnrollments",
                column: "StudentId");

            migrationBuilder.CreateIndex(
                name: "IX_StudentClassEnrollments_Student_Class",
                table: "StudentClassEnrollments",
                columns: new[] { "TenantId", "SchoolId", "StudentId", "ClassGroupId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SyncErrors_OccurredAt",
                table: "SyncErrors",
                columns: new[] { "TenantId", "SchoolId", "OccurredAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_SyncErrors_SyncRun",
                table: "SyncErrors",
                column: "SyncRunId");

            migrationBuilder.CreateIndex(
                name: "IX_SyncMetrics_SyncRun",
                table: "SyncMetrics",
                column: "SyncRunId");

            migrationBuilder.CreateIndex(
                name: "IX_SyncPayloadArchives_ExpiresAt",
                table: "SyncPayloadArchives",
                columns: new[] { "TenantId", "SchoolId", "ExpiresAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_SyncPayloadArchives_SyncRun",
                table: "SyncPayloadArchives",
                column: "SyncRunId");

            migrationBuilder.CreateIndex(
                name: "IX_SyncRuns_Provider_Started",
                table: "SyncRuns",
                columns: new[] { "TenantId", "SchoolId", "ProviderId", "StartedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_SyncRuns_Status",
                table: "SyncRuns",
                columns: new[] { "TenantId", "SchoolId", "Status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ReasonCodeMappings");

            migrationBuilder.DropTable(
                name: "SchoolSyncStates");

            migrationBuilder.DropTable(
                name: "StudentClassEnrollments");

            migrationBuilder.DropTable(
                name: "SyncErrors");

            migrationBuilder.DropTable(
                name: "SyncMetrics");

            migrationBuilder.DropTable(
                name: "SyncPayloadArchives");

            migrationBuilder.DropTable(
                name: "ClassGroups");

            migrationBuilder.DropTable(
                name: "SyncRuns");

            migrationBuilder.DropColumn(
                name: "ErrorRateThreshold",
                table: "IngestionSyncLogs");

            migrationBuilder.DropColumn(
                name: "MismatchDetailsJson",
                table: "IngestionSyncLogs");

            migrationBuilder.DropColumn(
                name: "MismatchThreshold",
                table: "IngestionSyncLogs");
        }
    }
}
