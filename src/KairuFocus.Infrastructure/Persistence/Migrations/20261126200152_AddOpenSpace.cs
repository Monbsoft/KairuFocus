using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KairuFocus.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddOpenSpace : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AmbianceCode",
                table: "MemberProfiles",
                type: "nvarchar(40)",
                maxLength: 40,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PresenceConsentedAtUtc",
                table: "MemberProfiles",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "MemberPresences",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LastSeenAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MemberPresences", x => x.UserId);
                    table.ForeignKey(
                        name: "FK_MemberPresences_MemberProfiles_UserId",
                        column: x => x.UserId,
                        principalTable: "MemberProfiles",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MemberPresences_LastSeenAtUtc",
                table: "MemberPresences",
                column: "LastSeenAtUtc");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MemberPresences");

            migrationBuilder.DropColumn(
                name: "AmbianceCode",
                table: "MemberProfiles");

            migrationBuilder.DropColumn(
                name: "PresenceConsentedAtUtc",
                table: "MemberProfiles");
        }
    }
}
