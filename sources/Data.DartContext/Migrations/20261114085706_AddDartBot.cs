using System;
using Microsoft.EntityFrameworkCore.Migrations;
using MySql.EntityFrameworkCore.Metadata;

#nullable disable

namespace Data.DartContext.Migrations
{
    /// <inheritdoc />
    public partial class AddDartBot : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DartBots",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    Name = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                    Alias = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false),
                    IsActive = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    ExperiencePoints = table.Column<long>(type: "bigint", nullable: false),
                    DevelopmentLevel = table.Column<int>(type: "int", nullable: false),
                    Rating = table.Column<int>(type: "int", nullable: false, defaultValue: 1000),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    CreatedBy = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    UpdatedBy = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DartBots", x => x.Id);
                    table.CheckConstraint("CK_DartBot_DevelopmentLevel", "`DevelopmentLevel` >= 0");
                    table.CheckConstraint("CK_DartBot_ExperiencePoints", "`ExperiencePoints` >= 0");
                    table.CheckConstraint("CK_DartBot_Rating", "`Rating` >= 0");
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "DartBotActiveModels",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    ModelType = table.Column<int>(type: "int", nullable: false),
                    ModelId = table.Column<int>(type: "int", nullable: false),
                    ActivatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    ActivatedBy = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    CreatedBy = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    UpdatedBy = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DartBotActiveModels", x => x.Id);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "DartBotGameLegs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    DartBotGameId = table.Column<int>(type: "int", nullable: false),
                    LegNumber = table.Column<int>(type: "int", nullable: false),
                    StartingPlayerId = table.Column<int>(type: "int", nullable: false),
                    WinnerPlayerId = table.Column<int>(type: "int", nullable: true),
                    StartedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    FinishedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    CreatedBy = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    UpdatedBy = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DartBotGameLegs", x => x.Id);
                    table.UniqueConstraint("AK_DartBotGameLegs_DartBotGameId_Id", x => new { x.DartBotGameId, x.Id });
                    table.CheckConstraint("CK_DartBotGameLeg_FinishedAt", "`FinishedAt` IS NULL OR `FinishedAt` >= `StartedAt`");
                    table.CheckConstraint("CK_DartBotGameLeg_LegNumber", "`LegNumber` >= 1");
                    table.CheckConstraint("CK_DartBotGameLeg_Winner", "`WinnerPlayerId` IS NULL OR `FinishedAt` IS NOT NULL");
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "DartBotGamePlayers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    DartBotGameId = table.Column<int>(type: "int", nullable: false),
                    PlayerNumber = table.Column<int>(type: "int", nullable: false),
                    PlayerType = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<int>(type: "int", nullable: true),
                    DartBotId = table.Column<int>(type: "int", nullable: true),
                    ProfileVersionId = table.Column<int>(type: "int", nullable: true),
                    TargetModelId = table.Column<int>(type: "int", nullable: true),
                    ThrowModelId = table.Column<int>(type: "int", nullable: true),
                    DisplayName = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                    XpGained = table.Column<long>(type: "bigint", nullable: true),
                    RatingBefore = table.Column<int>(type: "int", nullable: true),
                    RatingAfter = table.Column<int>(type: "int", nullable: true),
                    LearningProcessedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    CreatedBy = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    UpdatedBy = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DartBotGamePlayers", x => x.Id);
                    table.UniqueConstraint("AK_DartBotGamePlayers_DartBotGameId_Id", x => new { x.DartBotGameId, x.Id });
                    table.CheckConstraint("CK_DartBotGamePlayer_PlayerNumber", "`PlayerNumber` IN (1, 2)");
                    table.CheckConstraint("CK_DartBotGamePlayer_PlayerType", "(`PlayerType` = 2 AND `DartBotId` IS NOT NULL AND `ProfileVersionId` IS NOT NULL AND `UserId` IS NULL) OR (`PlayerType` = 1 AND `DartBotId` IS NULL AND `ProfileVersionId` IS NULL AND `TargetModelId` IS NULL AND `ThrowModelId` IS NULL AND `XpGained` IS NULL AND `RatingBefore` IS NULL AND `RatingAfter` IS NULL AND `LearningProcessedAt` IS NULL)");
                    table.CheckConstraint("CK_DartBotGamePlayer_XpGained", "`XpGained` IS NULL OR `XpGained` >= 0");
                    table.ForeignKey(
                        name: "FK_DartBotGamePlayers_DartBots_DartBotId",
                        column: x => x.DartBotId,
                        principalTable: "DartBots",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DartBotGamePlayers_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "DartBotGames",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    GameMode = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    StartingScore = table.Column<int>(type: "int", nullable: false),
                    BestOfLegs = table.Column<int>(type: "int", nullable: false),
                    WinnerPlayerId = table.Column<int>(type: "int", nullable: true),
                    StartedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    FinishedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    CreatedBy = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    UpdatedBy = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DartBotGames", x => x.Id);
                    table.CheckConstraint("CK_DartBotGame_BestOfLegs", "`BestOfLegs` > 0 AND `BestOfLegs` % 2 = 1");
                    table.CheckConstraint("CK_DartBotGame_FinishedAt", "`FinishedAt` IS NULL OR `FinishedAt` >= `StartedAt`");
                    table.CheckConstraint("CK_DartBotGame_StartingScore", "`StartingScore` IN (301, 501)");
                    table.CheckConstraint("CK_DartBotGame_StatusFinishedAt", "(`Status` = 1 AND `FinishedAt` IS NULL) OR (`Status` <> 1 AND `FinishedAt` IS NOT NULL)");
                    table.CheckConstraint("CK_DartBotGame_Winner", "`WinnerPlayerId` IS NULL OR `Status` = 2");
                    table.ForeignKey(
                        name: "FK_DartBotGames_DartBotGamePlayers_WinnerPlayerId",
                        column: x => x.WinnerPlayerId,
                        principalTable: "DartBotGamePlayers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "DartBotGameVisits",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    DartBotGameId = table.Column<int>(type: "int", nullable: false),
                    DartBotGameLegId = table.Column<int>(type: "int", nullable: false),
                    DartBotGamePlayerId = table.Column<int>(type: "int", nullable: false),
                    TurnNumber = table.Column<int>(type: "int", nullable: false),
                    VisitNumber = table.Column<int>(type: "int", nullable: false),
                    ScoreBefore = table.Column<int>(type: "int", nullable: false),
                    ScoreAfter = table.Column<int>(type: "int", nullable: false),
                    OpponentScoreBefore = table.Column<int>(type: "int", nullable: false),
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
                    table.PrimaryKey("PK_DartBotGameVisits", x => x.Id);
                    table.CheckConstraint("CK_DartBotGameVisit_BustCheckout", "NOT (`IsBust` = 1 AND `IsCheckout` = 1)");
                    table.CheckConstraint("CK_DartBotGameVisit_Checkout", "(`IsCheckout` = 1 AND `ScoreAfter` = 0 AND `DartsUsed` >= 1) OR (`IsCheckout` = 0 AND `ScoreAfter` > 0)");
                    table.CheckConstraint("CK_DartBotGameVisit_DartsUsed", "`DartsUsed` BETWEEN 0 AND 3");
                    table.CheckConstraint("CK_DartBotGameVisit_OpponentScoreBefore", "`OpponentScoreBefore` BETWEEN 2 AND 501");
                    table.CheckConstraint("CK_DartBotGameVisit_ScoreAfter", "`ScoreAfter` BETWEEN 0 AND `ScoreBefore` AND `ScoreBefore` - `ScoreAfter` <= 180 AND (`IsBust` = 0 OR `ScoreAfter` = `ScoreBefore`)");
                    table.CheckConstraint("CK_DartBotGameVisit_ScoreBefore", "`ScoreBefore` BETWEEN 2 AND 501");
                    table.CheckConstraint("CK_DartBotGameVisit_TurnNumber", "`TurnNumber` >= 1");
                    table.CheckConstraint("CK_DartBotGameVisit_VisitNumber", "`VisitNumber` >= 1");
                    table.ForeignKey(
                        name: "FK_DartBotGameVisits_DartBotGameLegs_DartBotGameId_DartBotGameL~",
                        columns: x => new { x.DartBotGameId, x.DartBotGameLegId },
                        principalTable: "DartBotGameLegs",
                        principalColumns: new[] { "DartBotGameId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DartBotGameVisits_DartBotGamePlayers_DartBotGameId_DartBotGa~",
                        columns: x => new { x.DartBotGameId, x.DartBotGamePlayerId },
                        principalTable: "DartBotGamePlayers",
                        principalColumns: new[] { "DartBotGameId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "DartBotGameThrows",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    DartBotGameVisitId = table.Column<int>(type: "int", nullable: false),
                    ThrowNumber = table.Column<int>(type: "int", nullable: false),
                    TargetSegment = table.Column<int>(type: "int", nullable: true),
                    TargetMultiplier = table.Column<int>(type: "int", nullable: true),
                    TargetX = table.Column<float>(type: "float", nullable: true),
                    TargetY = table.Column<float>(type: "float", nullable: true),
                    Segment = table.Column<int>(type: "int", nullable: false),
                    Multiplier = table.Column<int>(type: "int", nullable: false),
                    Score = table.Column<int>(type: "int", nullable: false),
                    HitX = table.Column<float>(type: "float", nullable: true),
                    HitY = table.Column<float>(type: "float", nullable: true),
                    ScoreBeforeThrow = table.Column<int>(type: "int", nullable: false),
                    DartsLeftInVisit = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    CreatedBy = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    UpdatedBy = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DartBotGameThrows", x => x.Id);
                    table.CheckConstraint("CK_DartBotGameThrow_DartsLeftInVisit", "`DartsLeftInVisit` = 3 - `ThrowNumber`");
                    table.CheckConstraint("CK_DartBotGameThrow_HitXY", "(`HitX` IS NULL) = (`HitY` IS NULL)");
                    table.CheckConstraint("CK_DartBotGameThrow_Score", "`Score` = `Segment` * `Multiplier`");
                    table.CheckConstraint("CK_DartBotGameThrow_ScoreBeforeThrow", "`ScoreBeforeThrow` BETWEEN 2 AND 501");
                    table.CheckConstraint("CK_DartBotGameThrow_SegmentMultiplier", "((`Segment` BETWEEN 1 AND 20 AND `Multiplier` BETWEEN 1 AND 3) OR (`Segment` = 25 AND `Multiplier` BETWEEN 1 AND 2) OR (`Segment` = 0 AND `Multiplier` = 0))");
                    table.CheckConstraint("CK_DartBotGameThrow_Target", "(`TargetSegment` IS NULL AND `TargetMultiplier` IS NULL) OR ((`TargetSegment` BETWEEN 1 AND 20 AND `TargetMultiplier` BETWEEN 1 AND 3) OR (`TargetSegment` = 25 AND `TargetMultiplier` BETWEEN 1 AND 2))");
                    table.CheckConstraint("CK_DartBotGameThrow_TargetXY", "(`TargetX` IS NULL) = (`TargetY` IS NULL)");
                    table.CheckConstraint("CK_DartBotGameThrow_ThrowNumber", "`ThrowNumber` BETWEEN 1 AND 3");
                    table.ForeignKey(
                        name: "FK_DartBotGameThrows_DartBotGameVisits_DartBotGameVisitId",
                        column: x => x.DartBotGameVisitId,
                        principalTable: "DartBotGameVisits",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "DartBotModels",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    ModelType = table.Column<int>(type: "int", nullable: false),
                    VersionNumber = table.Column<int>(type: "int", nullable: false),
                    TrainingId = table.Column<int>(type: "int", nullable: false),
                    StorageKey = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: false),
                    Format = table.Column<int>(type: "int", nullable: false),
                    SizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    Sha256 = table.Column<string>(type: "char(64)", fixedLength: true, maxLength: 64, nullable: false),
                    FeatureSchemaVersion = table.Column<int>(type: "int", nullable: false),
                    MlNetVersion = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    CreatedBy = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    UpdatedBy = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DartBotModels", x => x.Id);
                    table.UniqueConstraint("AK_DartBotModels_ModelType_Id", x => new { x.ModelType, x.Id });
                    table.CheckConstraint("CK_DartBotModel_SizeBytes", "`SizeBytes` >= 0");
                    table.CheckConstraint("CK_DartBotModel_VersionNumber", "`VersionNumber` >= 1");
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "DartBotProfileVersions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    DartBotId = table.Column<int>(type: "int", nullable: false),
                    VersionNumber = table.Column<int>(type: "int", nullable: false),
                    ThrowPrecision = table.Column<float>(type: "float", nullable: false),
                    ThrowConsistency = table.Column<float>(type: "float", nullable: false),
                    TacticalIntelligence = table.Column<float>(type: "float", nullable: false),
                    PressureResistance = table.Column<float>(type: "float", nullable: false),
                    RiskTaking = table.Column<float>(type: "float", nullable: false),
                    PlayingStyle = table.Column<int>(type: "int", nullable: true),
                    ChangeReason = table.Column<int>(type: "int", nullable: false),
                    ReasonText = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: true),
                    PreviousVersionId = table.Column<int>(type: "int", nullable: true),
                    TriggeringGameId = table.Column<int>(type: "int", nullable: true),
                    ModelId = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    CreatedBy = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    UpdatedBy = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DartBotProfileVersions", x => x.Id);
                    table.UniqueConstraint("AK_DartBotProfileVersions_DartBotId_Id", x => new { x.DartBotId, x.Id });
                    table.CheckConstraint("CK_DartBotProfileVersion_GameLearning", "`ChangeReason` <> 2 OR `TriggeringGameId` IS NOT NULL");
                    table.CheckConstraint("CK_DartBotProfileVersion_PressureResistance", "`PressureResistance` BETWEEN 0 AND 1");
                    table.CheckConstraint("CK_DartBotProfileVersion_RiskTaking", "`RiskTaking` BETWEEN 0 AND 1");
                    table.CheckConstraint("CK_DartBotProfileVersion_TacticalIntelligence", "`TacticalIntelligence` BETWEEN 0 AND 1");
                    table.CheckConstraint("CK_DartBotProfileVersion_ThrowConsistency", "`ThrowConsistency` BETWEEN 0 AND 1");
                    table.CheckConstraint("CK_DartBotProfileVersion_ThrowPrecision", "`ThrowPrecision` BETWEEN 0 AND 1");
                    table.CheckConstraint("CK_DartBotProfileVersion_VersionNumber", "`VersionNumber` >= 1");
                    table.ForeignKey(
                        name: "FK_DartBotProfileVersions_DartBotGames_TriggeringGameId",
                        column: x => x.TriggeringGameId,
                        principalTable: "DartBotGames",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DartBotProfileVersions_DartBotModels_ModelId",
                        column: x => x.ModelId,
                        principalTable: "DartBotModels",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DartBotProfileVersions_DartBotProfileVersions_PreviousVersio~",
                        column: x => x.PreviousVersionId,
                        principalTable: "DartBotProfileVersions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DartBotProfileVersions_DartBots_DartBotId",
                        column: x => x.DartBotId,
                        principalTable: "DartBots",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "DartBotTrainingDatasets",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    StorageKey = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: false),
                    Format = table.Column<int>(type: "int", nullable: false),
                    SourceType = table.Column<int>(type: "int", nullable: false),
                    RowCount = table.Column<long>(type: "bigint", nullable: false),
                    SizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    Sha256 = table.Column<string>(type: "char(64)", fixedLength: true, maxLength: 64, nullable: false),
                    FeatureSchemaVersion = table.Column<int>(type: "int", nullable: false),
                    GeneratorTargetModelId = table.Column<int>(type: "int", nullable: true),
                    GeneratorThrowModelId = table.Column<int>(type: "int", nullable: true),
                    RandomSeed = table.Column<int>(type: "int", nullable: true),
                    GeneratorParametersJson = table.Column<string>(type: "longtext", nullable: true),
                    FromGameId = table.Column<int>(type: "int", nullable: true),
                    ToGameId = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    CreatedBy = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    UpdatedBy = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DartBotTrainingDatasets", x => x.Id);
                    table.CheckConstraint("CK_DartBotTrainingDataset_GameRange", "`FromGameId` IS NULL OR `ToGameId` IS NULL OR `FromGameId` <= `ToGameId`");
                    table.CheckConstraint("CK_DartBotTrainingDataset_RowCount", "`RowCount` >= 0");
                    table.CheckConstraint("CK_DartBotTrainingDataset_SizeBytes", "`SizeBytes` >= 0");
                    table.ForeignKey(
                        name: "FK_DartBotTrainingDatasets_DartBotModels_GeneratorTargetModelId",
                        column: x => x.GeneratorTargetModelId,
                        principalTable: "DartBotModels",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DartBotTrainingDatasets_DartBotModels_GeneratorThrowModelId",
                        column: x => x.GeneratorThrowModelId,
                        principalTable: "DartBotModels",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "DartBotModelTrainings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    ModelType = table.Column<int>(type: "int", nullable: false),
                    DatasetId = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    StartedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    FinishedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    TrainingDataCount = table.Column<long>(type: "bigint", nullable: false),
                    MetricsJson = table.Column<string>(type: "longtext", nullable: true),
                    HyperparametersJson = table.Column<string>(type: "longtext", nullable: true),
                    TrainerName = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: true),
                    RandomSeed = table.Column<int>(type: "int", nullable: true),
                    MlNetVersion = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true),
                    CodeVersion = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: true),
                    FeatureSchemaVersion = table.Column<int>(type: "int", nullable: false),
                    ErrorMessage = table.Column<string>(type: "longtext", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    CreatedBy = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    UpdatedBy = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DartBotModelTrainings", x => x.Id);
                    table.CheckConstraint("CK_DartBotModelTraining_FinishedAt", "`Status` NOT IN (3, 4) OR `FinishedAt` IS NOT NULL");
                    table.CheckConstraint("CK_DartBotModelTraining_TrainingDataCount", "`TrainingDataCount` >= 0");
                    table.ForeignKey(
                        name: "FK_DartBotModelTrainings_DartBotTrainingDatasets_DatasetId",
                        column: x => x.DatasetId,
                        principalTable: "DartBotTrainingDatasets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_DartBotActiveModels_ModelType",
                table: "DartBotActiveModels",
                column: "ModelType",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DartBotActiveModels_ModelType_ModelId",
                table: "DartBotActiveModels",
                columns: new[] { "ModelType", "ModelId" });

            migrationBuilder.CreateIndex(
                name: "IX_DartBotGameLegs_DartBotGameId_LegNumber",
                table: "DartBotGameLegs",
                columns: new[] { "DartBotGameId", "LegNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DartBotGameLegs_DartBotGameId_StartingPlayerId",
                table: "DartBotGameLegs",
                columns: new[] { "DartBotGameId", "StartingPlayerId" });

            migrationBuilder.CreateIndex(
                name: "IX_DartBotGameLegs_DartBotGameId_WinnerPlayerId",
                table: "DartBotGameLegs",
                columns: new[] { "DartBotGameId", "WinnerPlayerId" });

            migrationBuilder.CreateIndex(
                name: "IX_DartBotGamePlayers_DartBotGameId_DartBotId",
                table: "DartBotGamePlayers",
                columns: new[] { "DartBotGameId", "DartBotId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DartBotGamePlayers_DartBotGameId_PlayerNumber",
                table: "DartBotGamePlayers",
                columns: new[] { "DartBotGameId", "PlayerNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DartBotGamePlayers_DartBotId_ProfileVersionId",
                table: "DartBotGamePlayers",
                columns: new[] { "DartBotId", "ProfileVersionId" });

            migrationBuilder.CreateIndex(
                name: "IX_DartBotGamePlayers_TargetModelId",
                table: "DartBotGamePlayers",
                column: "TargetModelId");

            migrationBuilder.CreateIndex(
                name: "IX_DartBotGamePlayers_ThrowModelId",
                table: "DartBotGamePlayers",
                column: "ThrowModelId");

            migrationBuilder.CreateIndex(
                name: "IX_DartBotGamePlayers_UserId",
                table: "DartBotGamePlayers",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_DartBotGames_GameMode_Status_FinishedAt",
                table: "DartBotGames",
                columns: new[] { "GameMode", "Status", "FinishedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_DartBotGames_WinnerPlayerId",
                table: "DartBotGames",
                column: "WinnerPlayerId");

            migrationBuilder.CreateIndex(
                name: "IX_DartBotGameThrows_DartBotGameVisitId_ThrowNumber",
                table: "DartBotGameThrows",
                columns: new[] { "DartBotGameVisitId", "ThrowNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DartBotGameVisits_DartBotGameId_DartBotGameLegId",
                table: "DartBotGameVisits",
                columns: new[] { "DartBotGameId", "DartBotGameLegId" });

            migrationBuilder.CreateIndex(
                name: "IX_DartBotGameVisits_DartBotGameId_DartBotGamePlayerId",
                table: "DartBotGameVisits",
                columns: new[] { "DartBotGameId", "DartBotGamePlayerId" });

            migrationBuilder.CreateIndex(
                name: "IX_DartBotGameVisits_DartBotGameLegId_DartBotGamePlayerId_Visit~",
                table: "DartBotGameVisits",
                columns: new[] { "DartBotGameLegId", "DartBotGamePlayerId", "VisitNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DartBotGameVisits_DartBotGameLegId_TurnNumber",
                table: "DartBotGameVisits",
                columns: new[] { "DartBotGameLegId", "TurnNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DartBotModels_ModelType_VersionNumber",
                table: "DartBotModels",
                columns: new[] { "ModelType", "VersionNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DartBotModels_StorageKey",
                table: "DartBotModels",
                column: "StorageKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DartBotModels_TrainingId",
                table: "DartBotModels",
                column: "TrainingId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DartBotModelTrainings_DatasetId",
                table: "DartBotModelTrainings",
                column: "DatasetId");

            migrationBuilder.CreateIndex(
                name: "IX_DartBotModelTrainings_ModelType_Status",
                table: "DartBotModelTrainings",
                columns: new[] { "ModelType", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_DartBotProfileVersions_DartBotId_VersionNumber",
                table: "DartBotProfileVersions",
                columns: new[] { "DartBotId", "VersionNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DartBotProfileVersions_ModelId",
                table: "DartBotProfileVersions",
                column: "ModelId");

            migrationBuilder.CreateIndex(
                name: "IX_DartBotProfileVersions_PreviousVersionId",
                table: "DartBotProfileVersions",
                column: "PreviousVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_DartBotProfileVersions_TriggeringGameId",
                table: "DartBotProfileVersions",
                column: "TriggeringGameId");

            migrationBuilder.CreateIndex(
                name: "IX_DartBots_Alias",
                table: "DartBots",
                column: "Alias",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DartBotTrainingDatasets_GeneratorTargetModelId",
                table: "DartBotTrainingDatasets",
                column: "GeneratorTargetModelId");

            migrationBuilder.CreateIndex(
                name: "IX_DartBotTrainingDatasets_GeneratorThrowModelId",
                table: "DartBotTrainingDatasets",
                column: "GeneratorThrowModelId");

            migrationBuilder.CreateIndex(
                name: "IX_DartBotTrainingDatasets_StorageKey",
                table: "DartBotTrainingDatasets",
                column: "StorageKey",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_DartBotActiveModels_DartBotModels_ModelType_ModelId",
                table: "DartBotActiveModels",
                columns: new[] { "ModelType", "ModelId" },
                principalTable: "DartBotModels",
                principalColumns: new[] { "ModelType", "Id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_DartBotGameLegs_DartBotGamePlayers_DartBotGameId_StartingPla~",
                table: "DartBotGameLegs",
                columns: new[] { "DartBotGameId", "StartingPlayerId" },
                principalTable: "DartBotGamePlayers",
                principalColumns: new[] { "DartBotGameId", "Id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_DartBotGameLegs_DartBotGamePlayers_DartBotGameId_WinnerPlaye~",
                table: "DartBotGameLegs",
                columns: new[] { "DartBotGameId", "WinnerPlayerId" },
                principalTable: "DartBotGamePlayers",
                principalColumns: new[] { "DartBotGameId", "Id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_DartBotGameLegs_DartBotGames_DartBotGameId",
                table: "DartBotGameLegs",
                column: "DartBotGameId",
                principalTable: "DartBotGames",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_DartBotGamePlayers_DartBotGames_DartBotGameId",
                table: "DartBotGamePlayers",
                column: "DartBotGameId",
                principalTable: "DartBotGames",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_DartBotGamePlayers_DartBotModels_TargetModelId",
                table: "DartBotGamePlayers",
                column: "TargetModelId",
                principalTable: "DartBotModels",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_DartBotGamePlayers_DartBotModels_ThrowModelId",
                table: "DartBotGamePlayers",
                column: "ThrowModelId",
                principalTable: "DartBotModels",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_DartBotGamePlayers_DartBotProfileVersions_DartBotId_ProfileV~",
                table: "DartBotGamePlayers",
                columns: new[] { "DartBotId", "ProfileVersionId" },
                principalTable: "DartBotProfileVersions",
                principalColumns: new[] { "DartBotId", "Id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_DartBotModels_DartBotModelTrainings_TrainingId",
                table: "DartBotModels",
                column: "TrainingId",
                principalTable: "DartBotModelTrainings",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DartBotGamePlayers_DartBotModels_TargetModelId",
                table: "DartBotGamePlayers");

            migrationBuilder.DropForeignKey(
                name: "FK_DartBotGamePlayers_DartBotModels_ThrowModelId",
                table: "DartBotGamePlayers");

            migrationBuilder.DropForeignKey(
                name: "FK_DartBotProfileVersions_DartBotModels_ModelId",
                table: "DartBotProfileVersions");

            migrationBuilder.DropForeignKey(
                name: "FK_DartBotTrainingDatasets_DartBotModels_GeneratorTargetModelId",
                table: "DartBotTrainingDatasets");

            migrationBuilder.DropForeignKey(
                name: "FK_DartBotTrainingDatasets_DartBotModels_GeneratorThrowModelId",
                table: "DartBotTrainingDatasets");

            migrationBuilder.DropForeignKey(
                name: "FK_DartBotGames_DartBotGamePlayers_WinnerPlayerId",
                table: "DartBotGames");

            migrationBuilder.DropTable(
                name: "DartBotActiveModels");

            migrationBuilder.DropTable(
                name: "DartBotGameThrows");

            migrationBuilder.DropTable(
                name: "DartBotGameVisits");

            migrationBuilder.DropTable(
                name: "DartBotGameLegs");

            migrationBuilder.DropTable(
                name: "DartBotModels");

            migrationBuilder.DropTable(
                name: "DartBotModelTrainings");

            migrationBuilder.DropTable(
                name: "DartBotTrainingDatasets");

            migrationBuilder.DropTable(
                name: "DartBotGamePlayers");

            migrationBuilder.DropTable(
                name: "DartBotProfileVersions");

            migrationBuilder.DropTable(
                name: "DartBotGames");

            migrationBuilder.DropTable(
                name: "DartBots");
        }
    }
}
