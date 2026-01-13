using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AnseoConnect.Data.Migrations
{
    /// <inheritdoc />
    public partial class Step9_AddMissingTablesManual : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Columns added to existing tables
            migrationBuilder.AddColumn<Guid>(
                name: "ETBTrustId",
                table: "Schools",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SyncErrorCount",
                table: "Schools",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "SyncStatus",
                table: "Schools",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<Guid>(
                name: "AssignedToUserId",
                table: "Cases",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BarrierCodes",
                table: "Cases",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "EscalatedAtUtc",
                table: "Cases",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ReviewDueAtUtc",
                table: "Cases",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "AcknowledgedAtUtc",
                table: "SafeguardingAlerts",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ChecklistProgress",
                table: "SafeguardingAlerts",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RoutedToUserIds",
                table: "SafeguardingAlerts",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Role",
                table: "AppUsers",
                type: "int",
                nullable: false,
                defaultValue: 0);

            // New tables
            migrationBuilder.CreateTable(
                name: "ETBTrusts",
                columns: table => new
                {
                    ETBTrustId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ETBTrusts", x => x.ETBTrustId);
                });

            migrationBuilder.CreateTable(
                name: "EvidencePacks",
                columns: table => new
                {
                    EvidencePackId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CaseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Format = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    StoragePath = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    GeneratedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SchoolId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EvidencePacks", x => x.EvidencePackId);
                    table.ForeignKey(
                        name: "FK_EvidencePacks_Cases_CaseId",
                        column: x => x.CaseId,
                        principalTable: "Cases",
                        principalColumn: "CaseId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "IngestionSyncLogs",
                columns: table => new
                {
                    IngestionSyncLogId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Source = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    StartedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CompletedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RecordsProcessed = table.Column<int>(type: "int", nullable: false),
                    ErrorCount = table.Column<int>(type: "int", nullable: false),
                    MismatchCount = table.Column<int>(type: "int", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SchoolId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IngestionSyncLogs", x => x.IngestionSyncLogId);
                });

            migrationBuilder.CreateTable(
                name: "MessageTemplates",
                columns: table => new
                {
                    MessageTemplateId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TemplateKey = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Channel = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Subject = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Body = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ToneConstraints = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MaxLength = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MessageTemplates", x => x.MessageTemplateId);
                });

            migrationBuilder.CreateTable(
                name: "NotificationRecipients",
                columns: table => new
                {
                    NotificationRecipientId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Route = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Role = table.Column<int>(type: "int", nullable: true),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Priority = table.Column<int>(type: "int", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SchoolId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NotificationRecipients", x => x.NotificationRecipientId);
                });

            migrationBuilder.CreateTable(
                name: "Notifications",
                columns: table => new
                {
                    NotificationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Type = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Payload = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ReadAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SchoolId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notifications", x => x.NotificationId);
                });

            migrationBuilder.CreateTable(
                name: "ReasonCodes",
                columns: table => new
                {
                    ReasonCodeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Label = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Type = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Scheme = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Version = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    IsDefault = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReasonCodes", x => x.ReasonCodeId);
                });

            migrationBuilder.CreateTable(
                name: "SchoolSettings",
                columns: table => new
                {
                    SchoolSettingsId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AMCutoffTime = table.Column<TimeOnly>(type: "time", nullable: false),
                    PMCutoffTime = table.Column<TimeOnly>(type: "time", nullable: false),
                    AutonomyLevel = table.Column<int>(type: "int", nullable: false),
                    PolicyPackIdOverride = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PolicyPackVersionOverride = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SchoolId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SchoolSettings", x => x.SchoolSettingsId);
                });

            migrationBuilder.CreateTable(
                name: "WorkTasks",
                columns: table => new
                {
                    WorkTaskId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CaseId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Title = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    AssignedToUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    AssignedRole = table.Column<int>(type: "int", nullable: true),
                    DueAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CompletedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ChecklistId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ChecklistProgress = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SchoolId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkTasks", x => x.WorkTaskId);
                    table.ForeignKey(
                        name: "FK_WorkTasks_Cases_CaseId",
                        column: x => x.CaseId,
                        principalTable: "Cases",
                        principalColumn: "CaseId",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "WorkTaskChecklists",
                columns: table => new
                {
                    WorkTaskChecklistId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WorkTaskId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ItemId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Label = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Required = table.Column<bool>(type: "bit", nullable: false),
                    Completed = table.Column<bool>(type: "bit", nullable: false),
                    CompletedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CompletedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SchoolId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkTaskChecklists", x => x.WorkTaskChecklistId);
                    table.ForeignKey(
                        name: "FK_WorkTaskChecklists_WorkTasks_WorkTaskId",
                        column: x => x.WorkTaskId,
                        principalTable: "WorkTasks",
                        principalColumn: "WorkTaskId",
                        onDelete: ReferentialAction.Cascade);
                });

            // Indexes
            migrationBuilder.CreateIndex(
                name: "IX_Schools_ETBTrustId",
                table: "Schools",
                column: "ETBTrustId");

            migrationBuilder.CreateIndex(
                name: "IX_ETBTrust_Tenant_Name",
                table: "ETBTrusts",
                columns: new[] { "TenantId", "Name" });

            migrationBuilder.CreateIndex(
                name: "IX_EvidencePacks_CaseId",
                table: "EvidencePacks",
                column: "CaseId");

            migrationBuilder.CreateIndex(
                name: "IX_IngestionSyncLogs_Tenant_School_Started",
                table: "IngestionSyncLogs",
                columns: new[] { "TenantId", "SchoolId", "StartedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_MessageTemplates_Tenant_Key_Channel",
                table: "MessageTemplates",
                columns: new[] { "TenantId", "TemplateKey", "Channel" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_NotificationRecipients_Route",
                table: "NotificationRecipients",
                columns: new[] { "TenantId", "SchoolId", "Route", "Priority" });

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_User_Created",
                table: "Notifications",
                columns: new[] { "TenantId", "SchoolId", "UserId", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_ReasonCodes_Tenant_Scheme_Version_Code",
                table: "ReasonCodes",
                columns: new[] { "TenantId", "Scheme", "Version", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SchoolSettings_Tenant_School",
                table: "SchoolSettings",
                columns: new[] { "TenantId", "SchoolId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WorkTaskChecklists_WorkTaskId",
                table: "WorkTaskChecklists",
                column: "WorkTaskId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkTasks_CaseId",
                table: "WorkTasks",
                column: "CaseId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkTasks_Status_Due",
                table: "WorkTasks",
                columns: new[] { "TenantId", "SchoolId", "Status", "DueAtUtc" });

            // FK from Schools to ETBTrusts
            migrationBuilder.AddForeignKey(
                name: "FK_Schools_ETBTrusts_ETBTrustId",
                table: "Schools",
                column: "ETBTrustId",
                principalTable: "ETBTrusts",
                principalColumn: "ETBTrustId",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Schools_ETBTrusts_ETBTrustId",
                table: "Schools");

            migrationBuilder.DropTable(
                name: "EvidencePacks");

            migrationBuilder.DropTable(
                name: "IngestionSyncLogs");

            migrationBuilder.DropTable(
                name: "MessageTemplates");

            migrationBuilder.DropTable(
                name: "NotificationRecipients");

            migrationBuilder.DropTable(
                name: "Notifications");

            migrationBuilder.DropTable(
                name: "ReasonCodes");

            migrationBuilder.DropTable(
                name: "SchoolSettings");

            migrationBuilder.DropTable(
                name: "WorkTaskChecklists");

            migrationBuilder.DropTable(
                name: "WorkTasks");

            migrationBuilder.DropTable(
                name: "ETBTrusts");

            migrationBuilder.DropIndex(
                name: "IX_Schools_ETBTrustId",
                table: "Schools");

            migrationBuilder.DropColumn(
                name: "ETBTrustId",
                table: "Schools");

            migrationBuilder.DropColumn(
                name: "SyncErrorCount",
                table: "Schools");

            migrationBuilder.DropColumn(
                name: "SyncStatus",
                table: "Schools");

            migrationBuilder.DropColumn(
                name: "AssignedToUserId",
                table: "Cases");

            migrationBuilder.DropColumn(
                name: "BarrierCodes",
                table: "Cases");

            migrationBuilder.DropColumn(
                name: "EscalatedAtUtc",
                table: "Cases");

            migrationBuilder.DropColumn(
                name: "ReviewDueAtUtc",
                table: "Cases");

            migrationBuilder.DropColumn(
                name: "AcknowledgedAtUtc",
                table: "SafeguardingAlerts");

            migrationBuilder.DropColumn(
                name: "ChecklistProgress",
                table: "SafeguardingAlerts");

            migrationBuilder.DropColumn(
                name: "RoutedToUserIds",
                table: "SafeguardingAlerts");

            migrationBuilder.DropColumn(
                name: "Role",
                table: "AppUsers");
        }
    }
}
