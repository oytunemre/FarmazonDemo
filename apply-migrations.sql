-- FarmazonDemo Database Migration Script
-- Add User Role Field

USE FarmazonDemodb;
GO

-- Check if Role column exists
IF NOT EXISTS (SELECT * FROM sys.columns
               WHERE object_id = OBJECT_ID(N'[dbo].[Users]')
               AND name = 'Role')
BEGIN
    -- Add Role column to Users table
    ALTER TABLE [dbo].[Users]
    ADD [Role] INT NOT NULL DEFAULT 0;

    PRINT 'Role column added successfully to Users table';
END
ELSE
BEGIN
    PRINT 'Role column already exists in Users table';
END
GO

-- Update migration history
IF NOT EXISTS (SELECT * FROM [dbo].[__EFMigrationsHistory]
               WHERE [MigrationId] = N'20260124000000_AddUserRoleField')
BEGIN
    INSERT INTO [dbo].[__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260124000000_AddUserRoleField', N'10.0.0');

    PRINT 'Migration history updated';
END
GO

PRINT 'Migration completed successfully!';
GO
