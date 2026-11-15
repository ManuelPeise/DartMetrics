using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Data.DartContext.Migrations
{
    /// <inheritdoc />
    public partial class AllowUserRatingOnBotGamePlayer : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_DartBotGamePlayer_PlayerType",
                table: "DartBotGamePlayers");

            migrationBuilder.AddCheckConstraint(
                name: "CK_DartBotGamePlayer_PlayerType",
                table: "DartBotGamePlayers",
                sql: "(`PlayerType` = 2 AND `DartBotId` IS NOT NULL AND `ProfileVersionId` IS NOT NULL AND `UserId` IS NULL) OR (`PlayerType` = 1 AND `DartBotId` IS NULL AND `ProfileVersionId` IS NULL AND `TargetModelId` IS NULL AND `ThrowModelId` IS NULL AND `XpGained` IS NULL)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_DartBotGamePlayer_PlayerType",
                table: "DartBotGamePlayers");

            migrationBuilder.AddCheckConstraint(
                name: "CK_DartBotGamePlayer_PlayerType",
                table: "DartBotGamePlayers",
                sql: "(`PlayerType` = 2 AND `DartBotId` IS NOT NULL AND `ProfileVersionId` IS NOT NULL AND `UserId` IS NULL) OR (`PlayerType` = 1 AND `DartBotId` IS NULL AND `ProfileVersionId` IS NULL AND `TargetModelId` IS NULL AND `ThrowModelId` IS NULL AND `XpGained` IS NULL AND `RatingBefore` IS NULL AND `RatingAfter` IS NULL AND `LearningProcessedAt` IS NULL)");
        }
    }
}
