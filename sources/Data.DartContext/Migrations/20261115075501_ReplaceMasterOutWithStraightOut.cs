using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Data.DartContext.Migrations
{
    /// <inheritdoc />
    public partial class ReplaceMasterOutWithStraightOut : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_TrainingGameVisit_ScoreBefore",
                table: "TrainingGameVisits");

            migrationBuilder.DropCheckConstraint(
                name: "CK_DartBotGameVisit_OpponentScoreBefore",
                table: "DartBotGameVisits");

            migrationBuilder.DropCheckConstraint(
                name: "CK_DartBotGameVisit_ScoreBefore",
                table: "DartBotGameVisits");

            migrationBuilder.DropCheckConstraint(
                name: "CK_DartBotGameThrow_ScoreBeforeThrow",
                table: "DartBotGameThrows");

            migrationBuilder.AddCheckConstraint(
                name: "CK_TrainingGameVisit_ScoreBefore",
                table: "TrainingGameVisits",
                sql: "`ScoreBefore` BETWEEN 1 AND 501");

            migrationBuilder.AddCheckConstraint(
                name: "CK_DartBotGameVisit_OpponentScoreBefore",
                table: "DartBotGameVisits",
                sql: "`OpponentScoreBefore` BETWEEN 1 AND 501");

            migrationBuilder.AddCheckConstraint(
                name: "CK_DartBotGameVisit_ScoreBefore",
                table: "DartBotGameVisits",
                sql: "`ScoreBefore` BETWEEN 1 AND 501");

            migrationBuilder.AddCheckConstraint(
                name: "CK_DartBotGameThrow_ScoreBeforeThrow",
                table: "DartBotGameThrows",
                sql: "`ScoreBeforeThrow` BETWEEN 1 AND 501");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_TrainingGameVisit_ScoreBefore",
                table: "TrainingGameVisits");

            migrationBuilder.DropCheckConstraint(
                name: "CK_DartBotGameVisit_OpponentScoreBefore",
                table: "DartBotGameVisits");

            migrationBuilder.DropCheckConstraint(
                name: "CK_DartBotGameVisit_ScoreBefore",
                table: "DartBotGameVisits");

            migrationBuilder.DropCheckConstraint(
                name: "CK_DartBotGameThrow_ScoreBeforeThrow",
                table: "DartBotGameThrows");

            migrationBuilder.AddCheckConstraint(
                name: "CK_TrainingGameVisit_ScoreBefore",
                table: "TrainingGameVisits",
                sql: "`ScoreBefore` BETWEEN 2 AND 501");

            migrationBuilder.AddCheckConstraint(
                name: "CK_DartBotGameVisit_OpponentScoreBefore",
                table: "DartBotGameVisits",
                sql: "`OpponentScoreBefore` BETWEEN 2 AND 501");

            migrationBuilder.AddCheckConstraint(
                name: "CK_DartBotGameVisit_ScoreBefore",
                table: "DartBotGameVisits",
                sql: "`ScoreBefore` BETWEEN 2 AND 501");

            migrationBuilder.AddCheckConstraint(
                name: "CK_DartBotGameThrow_ScoreBeforeThrow",
                table: "DartBotGameThrows",
                sql: "`ScoreBeforeThrow` BETWEEN 2 AND 501");
        }
    }
}
