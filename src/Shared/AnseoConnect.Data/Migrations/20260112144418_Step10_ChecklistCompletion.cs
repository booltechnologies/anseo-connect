using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AnseoConnect.Data.Migrations
{
    /// <inheritdoc />
    public partial class Step10_ChecklistCompletion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsOverride",
                table: "ReasonCodes",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Source",
                table: "ReasonCodes",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "ChecklistCompletions",
                columns: table => new
                {
                    ChecklistCompletionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CaseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ChecklistId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ItemId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    CompletedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CompletedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    WorkTaskId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    AlertId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SchoolId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChecklistCompletions", x => x.ChecklistCompletionId);
                    table.ForeignKey(
                        name: "FK_ChecklistCompletions_Cases_CaseId",
                        column: x => x.CaseId,
                        principalTable: "Cases",
                        principalColumn: "CaseId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ChecklistCompletions_SafeguardingAlerts_AlertId",
                        column: x => x.AlertId,
                        principalTable: "SafeguardingAlerts",
                        principalColumn: "AlertId");
                    table.ForeignKey(
                        name: "FK_ChecklistCompletions_WorkTasks_WorkTaskId",
                        column: x => x.WorkTaskId,
                        principalTable: "WorkTasks",
                        principalColumn: "WorkTaskId",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ChecklistCompletions_AlertId",
                table: "ChecklistCompletions",
                column: "AlertId");

            migrationBuilder.CreateIndex(
                name: "IX_ChecklistCompletions_Case_Item",
                table: "ChecklistCompletions",
                columns: new[] { "TenantId", "SchoolId", "CaseId", "ChecklistId", "ItemId" });

            migrationBuilder.CreateIndex(
                name: "IX_ChecklistCompletions_CaseId",
                table: "ChecklistCompletions",
                column: "CaseId");

            migrationBuilder.CreateIndex(
                name: "IX_ChecklistCompletions_WorkTaskId",
                table: "ChecklistCompletions",
                column: "WorkTaskId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ChecklistCompletions");

            migrationBuilder.DropColumn(
                name: "IsOverride",
                table: "ReasonCodes");

            migrationBuilder.DropColumn(
                name: "Source",
                table: "ReasonCodes");
        }
    }
}
