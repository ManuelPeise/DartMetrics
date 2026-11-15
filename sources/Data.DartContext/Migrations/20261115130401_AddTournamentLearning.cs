using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Data.DartContext.Migrations
{
    /// <inheritdoc />
    public partial class AddTournamentLearning : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AwayRatingAfter",
                table: "TournamentMatches",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "AwayRatingBefore",
                table: "TournamentMatches",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "AwayXpGained",
                table: "TournamentMatches",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "HomeRatingAfter",
                table: "TournamentMatches",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "HomeRatingBefore",
                table: "TournamentMatches",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "HomeXpGained",
                table: "TournamentMatches",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LearningProcessedAt",
                table: "TournamentMatches",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_TournamentMatch_Learning",
                table: "TournamentMatches",
                sql: "`LearningProcessedAt` IS NULL OR `Status` = 3");

            migrationBuilder.AddCheckConstraint(
                name: "CK_TournamentMatch_Xp",
                table: "TournamentMatches",
                sql: "(`HomeXpGained` IS NULL OR `HomeXpGained` >= 0) AND (`AwayXpGained` IS NULL OR `AwayXpGained` >= 0)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_TournamentMatch_Learning",
                table: "TournamentMatches");

            migrationBuilder.DropCheckConstraint(
                name: "CK_TournamentMatch_Xp",
                table: "TournamentMatches");

            migrationBuilder.DropColumn(
                name: "AwayRatingAfter",
                table: "TournamentMatches");

            migrationBuilder.DropColumn(
                name: "AwayRatingBefore",
                table: "TournamentMatches");

            migrationBuilder.DropColumn(
                name: "AwayXpGained",
                table: "TournamentMatches");

            migrationBuilder.DropColumn(
                name: "HomeRatingAfter",
                table: "TournamentMatches");

            migrationBuilder.DropColumn(
                name: "HomeRatingBefore",
                table: "TournamentMatches");

            migrationBuilder.DropColumn(
                name: "HomeXpGained",
                table: "TournamentMatches");

            migrationBuilder.DropColumn(
                name: "LearningProcessedAt",
                table: "TournamentMatches");
        }
    }
}
