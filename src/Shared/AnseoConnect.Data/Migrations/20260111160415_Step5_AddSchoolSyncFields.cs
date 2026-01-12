using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AnseoConnect.Data.Migrations
{
    /// <inheritdoc />
    public partial class Step5_AddSchoolSyncFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // LastSyncUtc and WondeDomain were already created in Step1_AddCoreEntities
            // This migration is now empty but kept for migration history consistency
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // LastSyncUtc and WondeDomain were already created in Step1_AddCoreEntities
            // This migration is now empty but kept for migration history consistency
        }
    }
}
