using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AnseoConnect.Data.Migrations
{
    /// <inheritdoc />
    public partial class EpicB_InterventionAnalytics : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "InterventionAnalytics",
                columns: table => new
                {
                    AnalyticsId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SchoolId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    TotalStudents = table.Column<int>(type: "int", nullable: false),
                    StudentsInIntervention = table.Column<int>(type: "int", nullable: false),
                    Letter1Sent = table.Column<int>(type: "int", nullable: false),
                    Letter2Sent = table.Column<int>(type: "int", nullable: false),
                    MeetingsScheduled = table.Column<int>(type: "int", nullable: false),
                    MeetingsHeld = table.Column<int>(type: "int", nullable: false),
                    Escalated = table.Column<int>(type: "int", nullable: false),
                    Resolved = table.Column<int>(type: "int", nullable: false),
                    PreInterventionAttendanceAvg = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    PostInterventionAttendanceAvg = table.Column<decimal>(type: "decimal(5,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InterventionAnalytics", x => x.AnalyticsId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_InterventionAnalytics_Date",
                table: "InterventionAnalytics",
                columns: new[] { "TenantId", "SchoolId", "Date" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "InterventionAnalytics");
        }
    }
}

