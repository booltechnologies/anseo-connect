using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AnseoConnect.Data.Migrations
{
    /// <inheritdoc />
    public partial class EpicE_StudentTimeline_Fixed : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TimelineEvents",
                columns: table => new
                {
                    EventId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StudentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CaseId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    EventType = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Category = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    SourceEntityType = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    SourceEntityId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OccurredAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ActorId = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ActorName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    Title = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    Summary = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MetadataJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    VisibilityScope = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    SearchableText = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SchoolId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TimelineEvents", x => x.EventId);
                    table.ForeignKey(
                        name: "FK_TimelineEvents_Cases_CaseId",
                        column: x => x.CaseId,
                        principalTable: "Cases",
                        principalColumn: "CaseId",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_TimelineEvents_Students_StudentId",
                        column: x => x.StudentId,
                        principalTable: "Students",
                        principalColumn: "StudentId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_StudentInterventionInstances_CaseId",
                table: "StudentInterventionInstances",
                column: "CaseId");

            migrationBuilder.CreateIndex(
                name: "IX_TimelineEvents_CaseId",
                table: "TimelineEvents",
                column: "CaseId");

            migrationBuilder.CreateIndex(
                name: "IX_TimelineEvents_CaseId_OccurredAtUtc",
                table: "TimelineEvents",
                columns: new[] { "TenantId", "CaseId", "OccurredAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_TimelineEvents_Category",
                table: "TimelineEvents",
                columns: new[] { "TenantId", "SchoolId", "StudentId", "Category", "OccurredAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_TimelineEvents_StudentId",
                table: "TimelineEvents",
                column: "StudentId");

            migrationBuilder.CreateIndex(
                name: "IX_TimelineEvents_StudentId_OccurredAtUtc",
                table: "TimelineEvents",
                columns: new[] { "TenantId", "SchoolId", "StudentId", "OccurredAtUtc" });

            migrationBuilder.AddForeignKey(
                name: "FK_StudentInterventionInstances_Cases_CaseId",
                table: "StudentInterventionInstances",
                column: "CaseId",
                principalTable: "Cases",
                principalColumn: "CaseId",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_StudentInterventionInstances_Cases_CaseId",
                table: "StudentInterventionInstances");

            migrationBuilder.DropTable(
                name: "TimelineEvents");

            migrationBuilder.DropIndex(
                name: "IX_StudentInterventionInstances_CaseId",
                table: "StudentInterventionInstances");
        }
    }
}
