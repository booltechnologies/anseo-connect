using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AnseoConnect.Data.Migrations
{
    /// <inheritdoc />
    public partial class Step4_AddCaseEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Cases",
                columns: table => new
                {
                    CaseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StudentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CaseType = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Tier = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ResolvedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ResolvedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ResolutionNotes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SchoolId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Cases", x => x.CaseId);
                    table.ForeignKey(
                        name: "FK_Cases_Students_StudentId",
                        column: x => x.StudentId,
                        principalTable: "Students",
                        principalColumn: "StudentId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CaseTimelineEvents",
                columns: table => new
                {
                    EventId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CaseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EventType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    EventData = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SchoolId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CaseTimelineEvents", x => x.EventId);
                    table.ForeignKey(
                        name: "FK_CaseTimelineEvents_Cases_CaseId",
                        column: x => x.CaseId,
                        principalTable: "Cases",
                        principalColumn: "CaseId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SafeguardingAlerts",
                columns: table => new
                {
                    AlertId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CaseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Severity = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ChecklistId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RequiresHumanReview = table.Column<bool>(type: "bit", nullable: false),
                    ReviewedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ReviewedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ReviewNotes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SchoolId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SafeguardingAlerts", x => x.AlertId);
                    table.ForeignKey(
                        name: "FK_SafeguardingAlerts_Cases_CaseId",
                        column: x => x.CaseId,
                        principalTable: "Cases",
                        principalColumn: "CaseId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Cases_StudentId",
                table: "Cases",
                column: "StudentId");

            migrationBuilder.CreateIndex(
                name: "IX_Cases_Tenant_School_Student_Status",
                table: "Cases",
                columns: new[] { "TenantId", "SchoolId", "StudentId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Cases_Tenant_School_Type_Status",
                table: "Cases",
                columns: new[] { "TenantId", "SchoolId", "CaseType", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_CaseTimelineEvents_CaseId",
                table: "CaseTimelineEvents",
                column: "CaseId");

            migrationBuilder.CreateIndex(
                name: "IX_CaseTimelineEvents_Tenant_School_Case_Created",
                table: "CaseTimelineEvents",
                columns: new[] { "TenantId", "SchoolId", "CaseId", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_SafeguardingAlerts_CaseId",
                table: "SafeguardingAlerts",
                column: "CaseId");

            migrationBuilder.CreateIndex(
                name: "IX_SafeguardingAlerts_Tenant_School_Case_Review",
                table: "SafeguardingAlerts",
                columns: new[] { "TenantId", "SchoolId", "CaseId", "RequiresHumanReview" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CaseTimelineEvents");

            migrationBuilder.DropTable(
                name: "SafeguardingAlerts");

            migrationBuilder.DropTable(
                name: "Cases");
        }
    }
}
