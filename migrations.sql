-- Kairudev Database Migration - SQL Server
-- Generated from EF Core Migration: 20260316175553_InitialCreate
-- This is the initial database schema for SQL Server (supporting only .NET 10 + SQL Server, no SQLite)

IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;

-- Create JournalEntries table
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'JournalEntries')
BEGIN
    CREATE TABLE [JournalEntries] (
        [Id] nvarchar(36) NOT NULL CONSTRAINT [PK_JournalEntries] PRIMARY KEY,
        [OwnerId] nvarchar(50) NULL,
        [OccurredAt] datetime2 NOT NULL,
        [EventType] nvarchar(max) NOT NULL,
        [ResourceId] nvarchar(36) NOT NULL,
        [Sequence] int NULL
    );
END;

-- Create PomodoroSessions table
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'PomodoroSessions')
BEGIN
    CREATE TABLE [PomodoroSessions] (
        [Id] nvarchar(36) NOT NULL CONSTRAINT [PK_PomodoroSessions] PRIMARY KEY,
        [OwnerId] nvarchar(50) NULL,
        [SessionType] nvarchar(max) NOT NULL,
        [Status] nvarchar(max) NOT NULL,
        [PlannedDurationMinutes] int NOT NULL,
        [StartedAt] datetime2 NULL,
        [EndedAt] datetime2 NULL,
        [LinkedTaskIds] nvarchar(max) NOT NULL
    );
END;

-- Create PomodoroSettings table
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'PomodoroSettings')
BEGIN
    CREATE TABLE [PomodoroSettings] (
        [UserId] nvarchar(50) NOT NULL CONSTRAINT [PK_PomodoroSettings] PRIMARY KEY,
        [SprintDurationMinutes] int NOT NULL,
        [ShortBreakDurationMinutes] int NOT NULL,
        [LongBreakDurationMinutes] int NOT NULL
    );
END;

-- Create Tasks table
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Tasks')
BEGIN
    CREATE TABLE [Tasks] (
        [Id] nvarchar(36) NOT NULL CONSTRAINT [PK_Tasks] PRIMARY KEY,
        [OwnerId] nvarchar(50) NULL,
        [Title] nvarchar(200) NOT NULL,
        [Description] nvarchar(1000) NULL,
        [Status] nvarchar(max) NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CompletedAt] datetime2 NULL,
        [JiraTicketKey] nvarchar(50) NULL
    );
END;

-- Create Users table
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Users')
BEGIN
    CREATE TABLE [Users] (
        [Id] nvarchar(50) NOT NULL CONSTRAINT [PK_Users] PRIMARY KEY,
        [GitHubId] nvarchar(50) NOT NULL,
        [Login] nvarchar(100) NOT NULL,
        [DisplayName] nvarchar(200) NOT NULL,
        [Email] nvarchar(200) NULL
    );
END;

-- Create unique index on Users.GitHubId
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Users_GitHubId')
BEGIN
    CREATE UNIQUE INDEX [IX_Users_GitHubId] ON [Users] ([GitHubId]);
END;

-- Create UserSettings table
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'UserSettings')
BEGIN
    CREATE TABLE [UserSettings] (
        [Id] nvarchar(50) NOT NULL CONSTRAINT [PK_UserSettings] PRIMARY KEY,
        [ThemePreference] nvarchar(20) NOT NULL,
        [RingtonePreference] nvarchar(20) NOT NULL DEFAULT 'AlarmClock',
        [JiraBaseUrl] nvarchar(500) NULL,
        [JiraEmail] nvarchar(200) NULL,
        [JiraApiToken] nvarchar(500) NULL
    );
END;

-- Create JournalComments table
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'JournalComments')
BEGIN
    CREATE TABLE [JournalComments] (
        [Id] nvarchar(36) NOT NULL CONSTRAINT [PK_JournalComments] PRIMARY KEY,
        [Text] nvarchar(1000) NOT NULL,
        [EntryId] nvarchar(36) NULL,
        CONSTRAINT [FK_JournalComments_JournalEntries_EntryId] FOREIGN KEY ([EntryId]) REFERENCES [JournalEntries] ([Id]) ON DELETE CASCADE
    );
END;

-- Create index on JournalComments.EntryId
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_JournalComments_EntryId')
BEGIN
    CREATE INDEX [IX_JournalComments_EntryId] ON [JournalComments] ([EntryId]);
END;

-- Record migration in history
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260316175553_InitialCreate'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260316175553_InitialCreate', N'10.0.3');
END;

COMMIT;
GO
