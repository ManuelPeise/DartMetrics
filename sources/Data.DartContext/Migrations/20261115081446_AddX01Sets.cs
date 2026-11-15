using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Data.DartContext.Migrations
{
    /// <inheritdoc />
    public partial class AddX01Sets : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "LegNumber",
                table: "TrainingGameVisits",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "NumberOfLegs",
                table: "TrainingGames",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddCheckConstraint(
                name: "CK_TrainingGameVisit_LegNumber",
                table: "TrainingGameVisits",
                sql: "`LegNumber` > 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_TrainingGame_NumberOfLegs",
                table: "TrainingGames",
                sql: "`NumberOfLegs` >= 1");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_TrainingGameVisit_LegNumber",
                table: "TrainingGameVisits");

            migrationBuilder.DropCheckConstraint(
                name: "CK_TrainingGame_NumberOfLegs",
                table: "TrainingGames");

            migrationBuilder.DropColumn(
                name: "LegNumber",
                table: "TrainingGameVisits");

            migrationBuilder.DropColumn(
                name: "NumberOfLegs",
                table: "TrainingGames");
        }
    }
}
