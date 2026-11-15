using System;
using Microsoft.EntityFrameworkCore.Migrations;
using MySql.EntityFrameworkCore.Metadata;

#nullable disable

namespace Data.DartContext.Migrations
{
    /// <inheritdoc />
    public partial class AddTournaments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TournamentMatches",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    TournamentRoundId = table.Column<int>(type: "int", nullable: false),
                    MatchNumber = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    HomeParticipantId = table.Column<int>(type: "int", nullable: true),
                    AwayParticipantId = table.Column<int>(type: "int", nullable: true),
                    HomeProfileVersionId = table.Column<int>(type: "int", nullable: true),
                    HomeTargetModelId = table.Column<int>(type: "int", nullable: true),
                    AwayProfileVersionId = table.Column<int>(type: "int", nullable: true),
                    AwayTargetModelId = table.Column<int>(type: "int", nullable: true),
                    HomeLegs = table.Column<int>(type: "int", nullable: false),
                    AwayLegs = table.Column<int>(type: "int", nullable: false),
                    WinnerParticipantId = table.Column<int>(type: "int", nullable: true),
                    NextMatchId = table.Column<int>(type: "int", nullable: true),
                    NextMatchSlot = table.Column<int>(type: "int", nullable: true),
                    ConductedByUserId = table.Column<int>(type: "int", nullable: true),
                    StartedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    FinishedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    ConcurrencyStamp = table.Column<Guid>(type: "char(36)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    CreatedBy = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    UpdatedBy = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TournamentMatches", x => x.Id);
                    table.CheckConstraint("CK_TournamentMatch_FinishedAt", "(`Status` = 3 AND `FinishedAt` IS NOT NULL) OR (`Status` <> 3 AND `FinishedAt` IS NULL)");
                    table.CheckConstraint("CK_TournamentMatch_Legs", "`HomeLegs` >= 0 AND `AwayLegs` >= 0");
                    table.CheckConstraint("CK_TournamentMatch_NextMatch", "(`NextMatchId` IS NULL AND `NextMatchSlot` IS NULL) OR (`NextMatchId` IS NOT NULL AND `NextMatchSlot` IN (1, 2))");
                    table.CheckConstraint("CK_TournamentMatch_Sides", "`HomeParticipantId` IS NULL OR `AwayParticipantId` IS NULL OR `HomeParticipantId` <> `AwayParticipantId`");
                    table.CheckConstraint("CK_TournamentMatch_Status", "(`Status` = 1) OR (`Status` IN (2, 3) AND `HomeParticipantId` IS NOT NULL AND `AwayParticipantId` IS NOT NULL AND `StartedAt` IS NOT NULL) OR (`Status` = 4 AND (`HomeParticipantId` IS NULL) <> (`AwayParticipantId` IS NULL))");
                    table.CheckConstraint("CK_TournamentMatch_Winner", "(`Status` IN (3, 4) AND `WinnerParticipantId` IS NOT NULL) OR (`Status` IN (1, 2) AND `WinnerParticipantId` IS NULL)");
                    table.ForeignKey(
                        name: "FK_TournamentMatches_DartBotModels_AwayTargetModelId",
                        column: x => x.AwayTargetModelId,
                        principalTable: "DartBotModels",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TournamentMatches_DartBotModels_HomeTargetModelId",
                        column: x => x.HomeTargetModelId,
                        principalTable: "DartBotModels",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TournamentMatches_DartBotProfileVersions_AwayProfileVersionId",
                        column: x => x.AwayProfileVersionId,
                        principalTable: "DartBotProfileVersions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TournamentMatches_DartBotProfileVersions_HomeProfileVersionId",
                        column: x => x.HomeProfileVersionId,
                        principalTable: "DartBotProfileVersions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TournamentMatches_TournamentMatches_NextMatchId",
                        column: x => x.NextMatchId,
                        principalTable: "TournamentMatches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TournamentMatches_Users_ConductedByUserId",
                        column: x => x.ConductedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "TournamentMatchLegs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    TournamentMatchId = table.Column<int>(type: "int", nullable: false),
                    LegNumber = table.Column<int>(type: "int", nullable: false),
                    StartingPlayerNumber = table.Column<int>(type: "int", nullable: false),
                    WinnerPlayerNumber = table.Column<int>(type: "int", nullable: true),
                    StartedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    FinishedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    CreatedBy = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    UpdatedBy = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TournamentMatchLegs", x => x.Id);
                    table.CheckConstraint("CK_TournamentMatchLeg_LegNumber", "`LegNumber` >= 1");
                    table.CheckConstraint("CK_TournamentMatchLeg_StartingPlayer", "`StartingPlayerNumber` IN (1, 2)");
                    table.CheckConstraint("CK_TournamentMatchLeg_Winner", "(`WinnerPlayerNumber` IS NULL AND `FinishedAt` IS NULL) OR (`WinnerPlayerNumber` IN (1, 2) AND `FinishedAt` IS NOT NULL)");
                    table.ForeignKey(
                        name: "FK_TournamentMatchLegs_TournamentMatches_TournamentMatchId",
                        column: x => x.TournamentMatchId,
                        principalTable: "TournamentMatches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "TournamentMatchVisits",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    TournamentMatchLegId = table.Column<int>(type: "int", nullable: false),
                    PlayerNumber = table.Column<int>(type: "int", nullable: false),
                    VisitNumber = table.Column<int>(type: "int", nullable: false),
                    ScoreBefore = table.Column<int>(type: "int", nullable: false),
                    ScoreAfter = table.Column<int>(type: "int", nullable: false),
                    IsBust = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    IsCheckout = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    DartsUsed = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    CreatedBy = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    UpdatedBy = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TournamentMatchVisits", x => x.Id);
                    table.CheckConstraint("CK_TournamentMatchVisit_BustCheckout", "NOT (`IsBust` = 1 AND `IsCheckout` = 1)");
                    table.CheckConstraint("CK_TournamentMatchVisit_Checkout", "(`IsCheckout` = 1 AND `ScoreAfter` = 0 AND `DartsUsed` >= 1) OR (`IsCheckout` = 0 AND `ScoreAfter` > 0)");
                    table.CheckConstraint("CK_TournamentMatchVisit_DartsUsed", "`DartsUsed` BETWEEN 0 AND 3");
                    table.CheckConstraint("CK_TournamentMatchVisit_PlayerNumber", "`PlayerNumber` IN (1, 2)");
                    table.CheckConstraint("CK_TournamentMatchVisit_ScoreAfter", "`ScoreAfter` BETWEEN 0 AND `ScoreBefore` AND `ScoreBefore` - `ScoreAfter` <= 180 AND (`IsBust` = 0 OR `ScoreAfter` = `ScoreBefore`)");
                    table.CheckConstraint("CK_TournamentMatchVisit_ScoreBefore", "`ScoreBefore` BETWEEN 1 AND 501");
                    table.CheckConstraint("CK_TournamentMatchVisit_VisitNumber", "`VisitNumber` >= 1");
                    table.ForeignKey(
                        name: "FK_TournamentMatchVisits_TournamentMatchLegs_TournamentMatchLeg~",
                        column: x => x.TournamentMatchLegId,
                        principalTable: "TournamentMatchLegs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "TournamentMatchThrows",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    TournamentMatchVisitId = table.Column<int>(type: "int", nullable: false),
                    ThrowNumber = table.Column<int>(type: "int", nullable: false),
                    TargetSegment = table.Column<int>(type: "int", nullable: true),
                    TargetMultiplier = table.Column<int>(type: "int", nullable: true),
                    Segment = table.Column<int>(type: "int", nullable: false),
                    Multiplier = table.Column<int>(type: "int", nullable: false),
                    Score = table.Column<int>(type: "int", nullable: false),
                    ScoreBeforeThrow = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    CreatedBy = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    UpdatedBy = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TournamentMatchThrows", x => x.Id);
                    table.CheckConstraint("CK_TournamentMatchThrow_Score", "`Score` = `Segment` * `Multiplier`");
                    table.CheckConstraint("CK_TournamentMatchThrow_SegmentMultiplier", "((`Segment` BETWEEN 1 AND 20 AND `Multiplier` BETWEEN 1 AND 3) OR (`Segment` = 25 AND `Multiplier` BETWEEN 1 AND 2) OR (`Segment` = 0 AND `Multiplier` = 0))");
                    table.CheckConstraint("CK_TournamentMatchThrow_Target", "(`TargetSegment` IS NULL AND `TargetMultiplier` IS NULL) OR ((`TargetSegment` BETWEEN 1 AND 20 AND `TargetMultiplier` BETWEEN 1 AND 3) OR (`TargetSegment` = 25 AND `TargetMultiplier` BETWEEN 1 AND 2))");
                    table.CheckConstraint("CK_TournamentMatchThrow_ThrowNumber", "`ThrowNumber` BETWEEN 1 AND 3");
                    table.ForeignKey(
                        name: "FK_TournamentMatchThrows_TournamentMatchVisits_TournamentMatchV~",
                        column: x => x.TournamentMatchVisitId,
                        principalTable: "TournamentMatchVisits",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "TournamentParticipants",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    TournamentId = table.Column<int>(type: "int", nullable: false),
                    ParticipantType = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<int>(type: "int", nullable: true),
                    DartBotId = table.Column<int>(type: "int", nullable: true),
                    DisplayName = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Seed = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    CreatedBy = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    UpdatedBy = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TournamentParticipants", x => x.Id);
                    table.CheckConstraint("CK_TournamentParticipant_Seed", "`Seed` IS NULL OR `Seed` >= 1");
                    table.CheckConstraint("CK_TournamentParticipant_Type", "(`ParticipantType` = 1 AND `DartBotId` IS NULL) OR (`ParticipantType` = 2 AND `DartBotId` IS NOT NULL AND `UserId` IS NULL)");
                    table.ForeignKey(
                        name: "FK_TournamentParticipants_DartBots_DartBotId",
                        column: x => x.DartBotId,
                        principalTable: "DartBots",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TournamentParticipants_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Tournaments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    Name = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    Kind = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    OwnerUserId = table.Column<int>(type: "int", nullable: true),
                    StartingScore = table.Column<int>(type: "int", nullable: false),
                    InMode = table.Column<int>(type: "int", nullable: false),
                    OutMode = table.Column<int>(type: "int", nullable: false),
                    RegistrationDeadline = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    SeedSourceTournamentId = table.Column<int>(type: "int", nullable: true),
                    SeedCount = table.Column<int>(type: "int", nullable: true),
                    StartedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    FinishedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    WinnerParticipantId = table.Column<int>(type: "int", nullable: true),
                    ConcurrencyStamp = table.Column<Guid>(type: "char(36)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    CreatedBy = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    UpdatedBy = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tournaments", x => x.Id);
                    table.CheckConstraint("CK_Tournament_FinishedAt", "(`Status` IN (4, 5) AND `FinishedAt` IS NOT NULL) OR (`Status` NOT IN (4, 5) AND `FinishedAt` IS NULL)");
                    table.CheckConstraint("CK_Tournament_Seeding", "(`SeedSourceTournamentId` IS NULL AND `SeedCount` IS NULL) OR (`Type` = 2 AND `SeedCount` IN (2, 4, 8))");
                    table.CheckConstraint("CK_Tournament_StartedAt", "`Status` NOT IN (3, 4) OR `StartedAt` IS NOT NULL");
                    table.CheckConstraint("CK_Tournament_StartingScore", "`StartingScore` IN (301, 501)");
                    table.CheckConstraint("CK_Tournament_Winner", "`WinnerParticipantId` IS NULL OR `Status` = 4");
                    table.ForeignKey(
                        name: "FK_Tournaments_TournamentParticipants_WinnerParticipantId",
                        column: x => x.WinnerParticipantId,
                        principalTable: "TournamentParticipants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Tournaments_Tournaments_SeedSourceTournamentId",
                        column: x => x.SeedSourceTournamentId,
                        principalTable: "Tournaments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Tournaments_Users_OwnerUserId",
                        column: x => x.OwnerUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "TournamentRounds",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    TournamentId = table.Column<int>(type: "int", nullable: false),
                    RoundNumber = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false),
                    BestOfLegs = table.Column<int>(type: "int", nullable: false),
                    ScheduledAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    CreatedBy = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    UpdatedBy = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TournamentRounds", x => x.Id);
                    table.CheckConstraint("CK_TournamentRound_BestOfLegs", "`BestOfLegs` IN (5, 7, 9, 13)");
                    table.CheckConstraint("CK_TournamentRound_RoundNumber", "`RoundNumber` >= 1");
                    table.ForeignKey(
                        name: "FK_TournamentRounds_Tournaments_TournamentId",
                        column: x => x.TournamentId,
                        principalTable: "Tournaments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_TournamentMatches_AwayParticipantId",
                table: "TournamentMatches",
                column: "AwayParticipantId");

            migrationBuilder.CreateIndex(
                name: "IX_TournamentMatches_AwayProfileVersionId",
                table: "TournamentMatches",
                column: "AwayProfileVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_TournamentMatches_AwayTargetModelId",
                table: "TournamentMatches",
                column: "AwayTargetModelId");

            migrationBuilder.CreateIndex(
                name: "IX_TournamentMatches_ConductedByUserId",
                table: "TournamentMatches",
                column: "ConductedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_TournamentMatches_HomeParticipantId",
                table: "TournamentMatches",
                column: "HomeParticipantId");

            migrationBuilder.CreateIndex(
                name: "IX_TournamentMatches_HomeProfileVersionId",
                table: "TournamentMatches",
                column: "HomeProfileVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_TournamentMatches_HomeTargetModelId",
                table: "TournamentMatches",
                column: "HomeTargetModelId");

            migrationBuilder.CreateIndex(
                name: "IX_TournamentMatches_NextMatchId_NextMatchSlot",
                table: "TournamentMatches",
                columns: new[] { "NextMatchId", "NextMatchSlot" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TournamentMatches_TournamentRoundId_MatchNumber",
                table: "TournamentMatches",
                columns: new[] { "TournamentRoundId", "MatchNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TournamentMatches_WinnerParticipantId",
                table: "TournamentMatches",
                column: "WinnerParticipantId");

            migrationBuilder.CreateIndex(
                name: "IX_TournamentMatchLegs_TournamentMatchId_LegNumber",
                table: "TournamentMatchLegs",
                columns: new[] { "TournamentMatchId", "LegNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TournamentMatchThrows_TournamentMatchVisitId_ThrowNumber",
                table: "TournamentMatchThrows",
                columns: new[] { "TournamentMatchVisitId", "ThrowNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TournamentMatchVisits_TournamentMatchLegId_VisitNumber",
                table: "TournamentMatchVisits",
                columns: new[] { "TournamentMatchLegId", "VisitNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TournamentParticipants_DartBotId",
                table: "TournamentParticipants",
                column: "DartBotId");

            migrationBuilder.CreateIndex(
                name: "IX_TournamentParticipants_TournamentId_DartBotId",
                table: "TournamentParticipants",
                columns: new[] { "TournamentId", "DartBotId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TournamentParticipants_TournamentId_Seed",
                table: "TournamentParticipants",
                columns: new[] { "TournamentId", "Seed" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TournamentParticipants_TournamentId_UserId",
                table: "TournamentParticipants",
                columns: new[] { "TournamentId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TournamentParticipants_UserId",
                table: "TournamentParticipants",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_TournamentRounds_ScheduledAt",
                table: "TournamentRounds",
                column: "ScheduledAt");

            migrationBuilder.CreateIndex(
                name: "IX_TournamentRounds_TournamentId_RoundNumber",
                table: "TournamentRounds",
                columns: new[] { "TournamentId", "RoundNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Tournaments_Kind_Status",
                table: "Tournaments",
                columns: new[] { "Kind", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Tournaments_OwnerUserId",
                table: "Tournaments",
                column: "OwnerUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Tournaments_SeedSourceTournamentId",
                table: "Tournaments",
                column: "SeedSourceTournamentId");

            migrationBuilder.CreateIndex(
                name: "IX_Tournaments_WinnerParticipantId",
                table: "Tournaments",
                column: "WinnerParticipantId");

            migrationBuilder.AddForeignKey(
                name: "FK_TournamentMatches_TournamentParticipants_AwayParticipantId",
                table: "TournamentMatches",
                column: "AwayParticipantId",
                principalTable: "TournamentParticipants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_TournamentMatches_TournamentParticipants_HomeParticipantId",
                table: "TournamentMatches",
                column: "HomeParticipantId",
                principalTable: "TournamentParticipants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_TournamentMatches_TournamentParticipants_WinnerParticipantId",
                table: "TournamentMatches",
                column: "WinnerParticipantId",
                principalTable: "TournamentParticipants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_TournamentMatches_TournamentRounds_TournamentRoundId",
                table: "TournamentMatches",
                column: "TournamentRoundId",
                principalTable: "TournamentRounds",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_TournamentParticipants_Tournaments_TournamentId",
                table: "TournamentParticipants",
                column: "TournamentId",
                principalTable: "Tournaments",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Tournaments_TournamentParticipants_WinnerParticipantId",
                table: "Tournaments");

            migrationBuilder.DropTable(
                name: "TournamentMatchThrows");

            migrationBuilder.DropTable(
                name: "TournamentMatchVisits");

            migrationBuilder.DropTable(
                name: "TournamentMatchLegs");

            migrationBuilder.DropTable(
                name: "TournamentMatches");

            migrationBuilder.DropTable(
                name: "TournamentRounds");

            migrationBuilder.DropTable(
                name: "TournamentParticipants");

            migrationBuilder.DropTable(
                name: "Tournaments");
        }
    }
}
