using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KairuFocus.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddGroupSessions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "GroupSessions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CurrentStepNumber = table.Column<int>(type: "int", nullable: false),
                    CurrentStepType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    CurrentStepStartedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CurrentStepEndsAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SprintCount = table.Column<int>(type: "int", nullable: false),
                    EndedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EndReason = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    IsClosedToJoin = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    Version = table.Column<int>(type: "int", nullable: false),
                    HostId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GroupSessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GroupSessions_Users_HostId",
                        column: x => x.HostId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "GroupSessionMembers",
                columns: table => new
                {
                    GroupSessionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    JoinedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LeftAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GroupSessionMembers", x => new { x.GroupSessionId, x.UserId });
                    table.ForeignKey(
                        name: "FK_GroupSessionMembers_GroupSessions_GroupSessionId",
                        column: x => x.GroupSessionId,
                        principalTable: "GroupSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_GroupSessionMembers_UserId",
                table: "GroupSessionMembers",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_GroupSessions_HostId",
                table: "GroupSessions",
                column: "HostId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Defensive: DROP TABLE IF EXISTS (SQL Server 2016+ and Azure SQL), so a replay on a partially
            // rolled-back database does not fail. Order matters: the members table references GroupSessions.
            // DATA LOSS, inherent to the rollback: all group sessions and their members are deleted.
            migrationBuilder.Sql("DROP TABLE IF EXISTS [GroupSessionMembers];");
            migrationBuilder.Sql("DROP TABLE IF EXISTS [GroupSessions];");
        }
    }
}
