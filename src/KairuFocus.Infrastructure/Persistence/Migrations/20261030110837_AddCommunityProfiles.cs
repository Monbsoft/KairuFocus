using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KairuFocus.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCommunityProfiles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // DATA LOSS, intended: the opt-in leaderboard profiles (pseudonym, join date) are replaced by
            // member profiles named after the user, and they are not carried over. Defensive form:
            // DROP TABLE IF EXISTS (SQL Server 2016+ and Azure SQL). The table is a leaf: its primary key,
            // foreign key and unique index go with it. Back up the table before applying this in production.
            migrationBuilder.Sql("DROP TABLE IF EXISTS [LeaderboardProfiles];");

            migrationBuilder.AddColumn<string>(
                name: "AvatarUrl",
                table: "Users",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            // The three shared sprint columns are additive and safe on existing rows: nullable, or a bit
            // with a default of 0 (a SQL Server metadata-only change).
            migrationBuilder.AddColumn<bool>(
                name: "IsClosedToJoin",
                table: "PomodoroSessions",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "SharedEndAt",
                table: "PomodoroSessions",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "SharedSprintId",
                table: "PomodoroSessions",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "MemberProfiles",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProfileName = table.Column<string>(type: "nvarchar(39)", maxLength: 39, nullable: false),
                    NameKey = table.Column<string>(type: "nvarchar(39)", maxLength: 39, nullable: false),
                    NameSource = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    AvatarUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    JoinedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MemberProfiles", x => x.UserId);
                    table.ForeignKey(
                        name: "FK_MemberProfiles_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            // No foreign key on SharedSprintId: see PomodoroSessionConfiguration.
            migrationBuilder.CreateIndex(
                name: "IX_PomodoroSessions_SharedSprintId",
                table: "PomodoroSessions",
                column: "SharedSprintId");

            migrationBuilder.CreateIndex(
                name: "IX_MemberProfiles_NameKey",
                table: "MemberProfiles",
                column: "NameKey",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Defensive: every step checks existence first, so a replay on a partially rolled-back database
            // does not fail. DATA LOSS, inherent to the rollback: all member profiles, the avatars on Users
            // and the shared sprint links on PomodoroSessions are deleted, and LeaderboardProfiles comes back
            // EMPTY (the rows dropped by Up are not recoverable from this migration).
            migrationBuilder.Sql("DROP TABLE IF EXISTS [MemberProfiles];");

            migrationBuilder.Sql("DROP INDEX IF EXISTS [IX_PomodoroSessions_SharedSprintId] ON [PomodoroSessions];");

            migrationBuilder.Sql("ALTER TABLE [Users] DROP COLUMN IF EXISTS [AvatarUrl];");
            migrationBuilder.Sql("ALTER TABLE [PomodoroSessions] DROP COLUMN IF EXISTS [SharedEndAt];");
            migrationBuilder.Sql("ALTER TABLE [PomodoroSessions] DROP COLUMN IF EXISTS [SharedSprintId];");

            // IsClosedToJoin carries a default constraint with a generated name, which must go first.
            migrationBuilder.Sql(@"
IF COL_LENGTH(N'PomodoroSessions', N'IsClosedToJoin') IS NOT NULL
BEGIN
    DECLARE @constraintName sysname;
    SELECT @constraintName = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c]
        ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE [d].[parent_object_id] = OBJECT_ID(N'[PomodoroSessions]') AND [c].[name] = N'IsClosedToJoin';
    IF @constraintName IS NOT NULL EXEC(N'ALTER TABLE [PomodoroSessions] DROP CONSTRAINT [' + @constraintName + N'];');
    ALTER TABLE [PomodoroSessions] DROP COLUMN [IsClosedToJoin];
END");

            // Same shape as AddLeaderboardProfiles: primary key, cascade foreign key, unique pseudonym index.
            migrationBuilder.Sql(@"
IF OBJECT_ID(N'[LeaderboardProfiles]', N'U') IS NULL
BEGIN
    CREATE TABLE [LeaderboardProfiles] (
        [UserId] uniqueidentifier NOT NULL,
        [Pseudonym] nvarchar(60) NOT NULL,
        [JoinedAtUtc] datetime2 NOT NULL,
        CONSTRAINT [PK_LeaderboardProfiles] PRIMARY KEY ([UserId]),
        CONSTRAINT [FK_LeaderboardProfiles_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
    );
    CREATE UNIQUE INDEX [IX_LeaderboardProfiles_Pseudonym] ON [LeaderboardProfiles] ([Pseudonym]);
END");
        }
    }
}
