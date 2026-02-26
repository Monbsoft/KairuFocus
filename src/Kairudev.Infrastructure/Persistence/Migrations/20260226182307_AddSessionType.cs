using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kairudev.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSessionType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SessionType",
                table: "PomodoroSessions",
                type: "TEXT",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SessionType",
                table: "PomodoroSessions");
        }
    }
}
