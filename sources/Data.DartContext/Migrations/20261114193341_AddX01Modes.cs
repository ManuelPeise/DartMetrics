using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Data.DartContext.Migrations
{
    /// <inheritdoc />
    public partial class AddX01Modes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "InMode",
                table: "TrainingGames",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "OutMode",
                table: "TrainingGames",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "InMode",
                table: "DartBotGames",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "OutMode",
                table: "DartBotGames",
                type: "int",
                nullable: false,
                defaultValue: 1);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "InMode",
                table: "TrainingGames");

            migrationBuilder.DropColumn(
                name: "OutMode",
                table: "TrainingGames");

            migrationBuilder.DropColumn(
                name: "InMode",
                table: "DartBotGames");

            migrationBuilder.DropColumn(
                name: "OutMode",
                table: "DartBotGames");
        }
    }
}
