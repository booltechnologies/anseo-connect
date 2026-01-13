using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AnseoConnect.Data.Migrations
{
    /// <inheritdoc />
    public partial class EpicB_AttendanceNormalization : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "RawPayloadJson",
                table: "AttendanceMarks",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AttendanceDailySummaries",
                columns: table => new
                {
                    SummaryId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SchoolId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StudentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    AMStatus = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    PMStatus = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    AMReasonCode = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    PMReasonCode = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    AttendancePercent = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    ConsecutiveAbsenceDays = table.Column<int>(type: "int", nullable: false),
                    TotalAbsenceDaysYTD = table.Column<int>(type: "int", nullable: false),
                    ComputedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AttendanceDailySummaries", x => x.SummaryId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceDailySummary_Date",
                table: "AttendanceDailySummaries",
                columns: new[] { "TenantId", "SchoolId", "Date" });

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceDailySummary_Student_Date",
                table: "AttendanceDailySummaries",
                columns: new[] { "TenantId", "SchoolId", "StudentId", "Date" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AttendanceDailySummaries");

            migrationBuilder.DropColumn(
                name: "RawPayloadJson",
                table: "AttendanceMarks");
        }
    }
}

