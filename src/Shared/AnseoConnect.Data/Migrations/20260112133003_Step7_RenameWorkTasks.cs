using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AnseoConnect.Data.Migrations
{
    /// <inheritdoc />
    public partial class Step7_RenameWorkTasks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // This migration renames Tasks/TaskChecklists to WorkTasks/WorkTaskChecklists.
            // If the old tables do not exist (fresh database already on new schema), skip.
            migrationBuilder.Sql(
                """
                IF OBJECT_ID(N'[TaskChecklists]') IS NOT NULL AND OBJECT_ID(N'[Tasks]') IS NOT NULL
                BEGIN
                    -- Drop old FK
                    IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_TaskChecklists_Tasks_TaskId')
                        ALTER TABLE [TaskChecklists] DROP CONSTRAINT [FK_TaskChecklists_Tasks_TaskId];

                    -- Rename tables
                    EXEC sp_rename 'Tasks', 'WorkTasks';
                    EXEC sp_rename 'TaskChecklists', 'WorkTaskChecklists';

                    -- Rename columns
                    EXEC sp_rename 'WorkTasks.TaskId', 'WorkTaskId', 'COLUMN';
                    EXEC sp_rename 'WorkTaskChecklists.TaskId', 'WorkTaskId', 'COLUMN';
                    EXEC sp_rename 'WorkTaskChecklists.TaskChecklistId', 'WorkTaskChecklistId', 'COLUMN';

                    -- Rename indexes
                    IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Tasks_Status_Due')
                        EXEC sp_rename 'IX_Tasks_Status_Due', 'IX_WorkTasks_Status_Due';
                    IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Tasks_CaseId')
                        EXEC sp_rename 'IX_Tasks_CaseId', 'IX_WorkTasks_CaseId';
                    IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_TaskChecklists_TaskId')
                        EXEC sp_rename 'IX_TaskChecklists_TaskId', 'IX_WorkTaskChecklists_WorkTaskId';

                    -- Recreate PKs
                    ALTER TABLE [WorkTasks] ADD CONSTRAINT [PK_WorkTasks] PRIMARY KEY ([WorkTaskId]);
                    ALTER TABLE [WorkTaskChecklists] ADD CONSTRAINT [PK_WorkTaskChecklists] PRIMARY KEY ([WorkTaskChecklistId]);

                    -- Recreate FK
                    ALTER TABLE [WorkTaskChecklists] WITH CHECK
                        ADD CONSTRAINT [FK_WorkTaskChecklists_WorkTasks_WorkTaskId]
                        FOREIGN KEY([WorkTaskId]) REFERENCES [WorkTasks] ([WorkTaskId]) ON DELETE CASCADE;
                END
                ELSE
                BEGIN
                    PRINT 'Task tables already migrated; skipping rename.';
                END
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Reverse only if the new tables exist
            migrationBuilder.Sql(
                """
                IF OBJECT_ID(N'[WorkTaskChecklists]') IS NOT NULL AND OBJECT_ID(N'[WorkTasks]') IS NOT NULL
                BEGIN
                    IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_WorkTaskChecklists_WorkTasks_WorkTaskId')
                        ALTER TABLE [WorkTaskChecklists] DROP CONSTRAINT [FK_WorkTaskChecklists_WorkTasks_WorkTaskId];

                    IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'PK_WorkTaskChecklists')
                        ALTER TABLE [WorkTaskChecklists] DROP CONSTRAINT [PK_WorkTaskChecklists];
                    IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'PK_WorkTasks')
                        ALTER TABLE [WorkTasks] DROP CONSTRAINT [PK_WorkTasks];

                    EXEC sp_rename 'WorkTasks', 'Tasks';
                    EXEC sp_rename 'WorkTaskChecklists', 'TaskChecklists';

                    EXEC sp_rename 'Tasks.WorkTaskId', 'TaskId', 'COLUMN';
                    EXEC sp_rename 'TaskChecklists.WorkTaskId', 'TaskId', 'COLUMN';
                    EXEC sp_rename 'TaskChecklists.WorkTaskChecklistId', 'TaskChecklistId', 'COLUMN';

                    IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_WorkTasks_Status_Due')
                        EXEC sp_rename 'IX_WorkTasks_Status_Due', 'IX_Tasks_Status_Due';
                    IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_WorkTasks_CaseId')
                        EXEC sp_rename 'IX_WorkTasks_CaseId', 'IX_Tasks_CaseId';
                    IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_WorkTaskChecklists_WorkTaskId')
                        EXEC sp_rename 'IX_WorkTaskChecklists_WorkTaskId', 'IX_TaskChecklists_TaskId';

                    ALTER TABLE [Tasks] ADD CONSTRAINT [PK_Tasks] PRIMARY KEY ([TaskId]);
                    ALTER TABLE [TaskChecklists] ADD CONSTRAINT [PK_TaskChecklists] PRIMARY KEY ([TaskChecklistId]);

                    ALTER TABLE [TaskChecklists] WITH CHECK
                        ADD CONSTRAINT [FK_TaskChecklists_Tasks_TaskId]
                        FOREIGN KEY([TaskId]) REFERENCES [Tasks] ([TaskId]) ON DELETE CASCADE;
                END
                ELSE
                BEGIN
                    PRINT 'WorkTask tables not found; skipping Down rename.';
                END
                """);
        }
    }
}
