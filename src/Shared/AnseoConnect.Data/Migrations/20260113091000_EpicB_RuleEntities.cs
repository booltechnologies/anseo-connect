using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AnseoConnect.Data.Migrations
{
    /// <inheritdoc />
    public partial class EpicB_RuleEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "InterventionRuleSets",
                columns: table => new
                {
                    RuleSetId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SchoolId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Jurisdiction = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    RulesJson = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InterventionRuleSets", x => x.RuleSetId);
                });

            migrationBuilder.CreateTable(
                name: "InterventionStages",
                columns: table => new
                {
                    StageId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RuleSetId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Order = table.Column<int>(type: "int", nullable: false),
                    StageType = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    LetterTemplateId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DaysBeforeNextStage = table.Column<int>(type: "int", nullable: true),
                    StopConditionsJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    EscalationConditionsJson = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InterventionStages", x => x.StageId);
                });

            migrationBuilder.CreateTable(
                name: "StudentInterventionInstances",
                columns: table => new
                {
                    InstanceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SchoolId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StudentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CaseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RuleSetId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CurrentStageId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    StartedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastStageAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StudentInterventionInstances", x => x.InstanceId);
                });

            migrationBuilder.CreateTable(
                name: "InterventionEvents",
                columns: table => new
                {
                    EventId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SchoolId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    InstanceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StageId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EventType = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ArtifactId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    OccurredAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InterventionEvents", x => x.EventId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_InterventionEvents_Instance_Time",
                table: "InterventionEvents",
                columns: new[] { "TenantId", "SchoolId", "InstanceId", "OccurredAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_InterventionInstances_RuleSet_Status",
                table: "StudentInterventionInstances",
                columns: new[] { "TenantId", "RuleSetId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_InterventionInstances_Student_Status",
                table: "StudentInterventionInstances",
                columns: new[] { "TenantId", "SchoolId", "StudentId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_InterventionRuleSets_Active",
                table: "InterventionRuleSets",
                columns: new[] { "TenantId", "SchoolId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_InterventionStages_RuleSet_Order",
                table: "InterventionStages",
                columns: new[] { "TenantId", "RuleSetId", "Order" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "InterventionEvents");

            migrationBuilder.DropTable(
                name: "InterventionStages");

            migrationBuilder.DropTable(
                name: "StudentInterventionInstances");

            migrationBuilder.DropTable(
                name: "InterventionRuleSets");
        }
    }
}

