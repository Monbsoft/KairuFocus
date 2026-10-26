using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KairuFocus.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddLeaderboardProfiles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LeaderboardProfiles",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Pseudonym = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    JoinedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LeaderboardProfiles", x => x.UserId);
                    table.ForeignKey(
                        name: "FK_LeaderboardProfiles_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LeaderboardProfiles_Pseudonym",
                table: "LeaderboardProfiles",
                column: "Pseudonym",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Defensive: DROP TABLE IF EXISTS (SQL Server 2016+ and Azure SQL) so a replay on a partially
            // rolled-back database does not fail. The table is a leaf (nothing references it), and its
            // primary key, foreign key and unique index go with it: no other table is touched.
            // Data loss is inherent to the rollback: every opt-in profile (pseudonym, join date) is deleted.
            migrationBuilder.Sql("DROP TABLE IF EXISTS [LeaderboardProfiles];");
        }
    }
}
