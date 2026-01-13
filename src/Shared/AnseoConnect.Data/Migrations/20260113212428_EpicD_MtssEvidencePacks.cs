using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AnseoConnect.Data.Migrations
{
    /// <inheritdoc />
    public partial class EpicD_MtssEvidencePacks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_EvidencePacks_CaseId",
                table: "EvidencePacks");

            migrationBuilder.AlterColumn<string>(
                name: "Format",
                table: "EvidencePacks",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<string>(
                name: "ContentHash",
                table: "EvidencePacks",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateOnly>(
                name: "DateRangeEnd",
                table: "EvidencePacks",
                type: "date",
                nullable: false,
                defaultValue: new DateOnly(1, 1, 1));

            migrationBuilder.AddColumn<DateOnly>(
                name: "DateRangeStart",
                table: "EvidencePacks",
                type: "date",
                nullable: false,
                defaultValue: new DateOnly(1, 1, 1));

            migrationBuilder.AddColumn<Guid>(
                name: "GeneratedByUserId",
                table: "EvidencePacks",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<string>(
                name: "GenerationPurpose",
                table: "EvidencePacks",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "IncludedSectionsJson",
                table: "EvidencePacks",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "IndexJson",
                table: "EvidencePacks",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ManifestHash",
                table: "EvidencePacks",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "StudentId",
                table: "EvidencePacks",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<string>(
                name: "ZipStoragePath",
                table: "EvidencePacks",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "MtssInterventions",
                columns: table => new
                {
                    InterventionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Category = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ApplicableTiersJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    EvidenceRequirementsJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RequiresParentConsent = table.Column<bool>(type: "bit", nullable: false),
                    TypicalDurationDays = table.Column<int>(type: "int", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MtssInterventions", x => x.InterventionId);
                });

            migrationBuilder.CreateTable(
                name: "MtssTierDefinitions",
                columns: table => new
                {
                    TierDefinitionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TierNumber = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    EntryCriteriaJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ExitCriteriaJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    EscalationCriteriaJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ReviewIntervalDays = table.Column<int>(type: "int", nullable: false),
                    RequiredArtifactsJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RecommendedInterventionsJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MtssTierDefinitions", x => x.TierDefinitionId);
                });

            migrationBuilder.CreateTable(
                name: "CaseInterventions",
                columns: table => new
                {
                    CaseInterventionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CaseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    InterventionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TierWhenApplied = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    StartedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CompletedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    OutcomeNotes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AssignedToUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SchoolId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CaseInterventions", x => x.CaseInterventionId);
                    table.ForeignKey(
                        name: "FK_CaseInterventions_Cases_CaseId",
                        column: x => x.CaseId,
                        principalTable: "Cases",
                        principalColumn: "CaseId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CaseInterventions_MtssInterventions_InterventionId",
                        column: x => x.InterventionId,
                        principalTable: "MtssInterventions",
                        principalColumn: "InterventionId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TierAssignments",
                columns: table => new
                {
                    AssignmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StudentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CaseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TierNumber = table.Column<int>(type: "int", nullable: false),
                    TierDefinitionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AssignmentReason = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    RationaleJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AssignedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    NextReviewAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    AssignedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SchoolId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TierAssignments", x => x.AssignmentId);
                    table.ForeignKey(
                        name: "FK_TierAssignments_Cases_CaseId",
                        column: x => x.CaseId,
                        principalTable: "Cases",
                        principalColumn: "CaseId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TierAssignments_MtssTierDefinitions_TierDefinitionId",
                        column: x => x.TierDefinitionId,
                        principalTable: "MtssTierDefinitions",
                        principalColumn: "TierDefinitionId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TierAssignmentHistories",
                columns: table => new
                {
                    HistoryId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AssignmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StudentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CaseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FromTier = table.Column<int>(type: "int", nullable: false),
                    ToTier = table.Column<int>(type: "int", nullable: false),
                    ChangeType = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    ChangeReason = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    RationaleJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ChangedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ChangedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SchoolId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TierAssignmentHistories", x => x.HistoryId);
                    table.ForeignKey(
                        name: "FK_TierAssignmentHistories_Cases_CaseId",
                        column: x => x.CaseId,
                        principalTable: "Cases",
                        principalColumn: "CaseId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TierAssignmentHistories_TierAssignments_AssignmentId",
                        column: x => x.AssignmentId,
                        principalTable: "TierAssignments",
                        principalColumn: "AssignmentId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EvidencePacks_Case_GeneratedAt",
                table: "EvidencePacks",
                columns: new[] { "CaseId", "GeneratedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_CaseInterventions_Case_Status",
                table: "CaseInterventions",
                columns: new[] { "CaseId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_CaseInterventions_InterventionId",
                table: "CaseInterventions",
                column: "InterventionId");

            migrationBuilder.CreateIndex(
                name: "IX_MtssInterventions_Tenant_Category",
                table: "MtssInterventions",
                columns: new[] { "TenantId", "Category" });

            migrationBuilder.CreateIndex(
                name: "IX_MtssTierDefinitions_Tenant_TierNumber",
                table: "MtssTierDefinitions",
                columns: new[] { "TenantId", "TierNumber" });

            migrationBuilder.CreateIndex(
                name: "IX_TierAssignmentHistories_AssignmentId",
                table: "TierAssignmentHistories",
                column: "AssignmentId");

            migrationBuilder.CreateIndex(
                name: "IX_TierAssignmentHistory_Case_ChangedAt",
                table: "TierAssignmentHistories",
                columns: new[] { "CaseId", "ChangedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_TierAssignments_Case",
                table: "TierAssignments",
                column: "CaseId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TierAssignments_TierDefinitionId",
                table: "TierAssignments",
                column: "TierDefinitionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CaseInterventions");

            migrationBuilder.DropTable(
                name: "TierAssignmentHistories");

            migrationBuilder.DropTable(
                name: "MtssInterventions");

            migrationBuilder.DropTable(
                name: "TierAssignments");

            migrationBuilder.DropTable(
                name: "MtssTierDefinitions");

            migrationBuilder.DropIndex(
                name: "IX_EvidencePacks_Case_GeneratedAt",
                table: "EvidencePacks");

            migrationBuilder.DropColumn(
                name: "ContentHash",
                table: "EvidencePacks");

            migrationBuilder.DropColumn(
                name: "DateRangeEnd",
                table: "EvidencePacks");

            migrationBuilder.DropColumn(
                name: "DateRangeStart",
                table: "EvidencePacks");

            migrationBuilder.DropColumn(
                name: "GeneratedByUserId",
                table: "EvidencePacks");

            migrationBuilder.DropColumn(
                name: "GenerationPurpose",
                table: "EvidencePacks");

            migrationBuilder.DropColumn(
                name: "IncludedSectionsJson",
                table: "EvidencePacks");

            migrationBuilder.DropColumn(
                name: "IndexJson",
                table: "EvidencePacks");

            migrationBuilder.DropColumn(
                name: "ManifestHash",
                table: "EvidencePacks");

            migrationBuilder.DropColumn(
                name: "StudentId",
                table: "EvidencePacks");

            migrationBuilder.DropColumn(
                name: "ZipStoragePath",
                table: "EvidencePacks");

            migrationBuilder.AlterColumn<string>(
                name: "Format",
                table: "EvidencePacks",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(32)",
                oldMaxLength: 32);

            migrationBuilder.CreateIndex(
                name: "IX_EvidencePacks_CaseId",
                table: "EvidencePacks",
                column: "CaseId");
        }
    }
}
