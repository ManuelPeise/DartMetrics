using System;
using Microsoft.EntityFrameworkCore.Migrations;
using MySql.EntityFrameworkCore.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Data.DartContext.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("MySQL:Charset", "utf8mb4");

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
                    PotentialAverage = table.Column<float>(type: "float", nullable: true),
                    Rating = table.Column<int>(type: "int", nullable: false, defaultValue: 1000),
                    ConcurrencyStamp = table.Column<Guid>(type: "char(36)", nullable: false),
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
                name: "LogMessages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    Message = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: false),
                    Module = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false),
                    ExceptionMessage = table.Column<string>(type: "text", nullable: true),
                    Stacktrace = table.Column<string>(type: "mediumtext", nullable: true),
                    LogMessageType = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    CreatedBy = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    UpdatedBy = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LogMessages", x => x.Id);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Roles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    UserRole = table.Column<int>(type: "int", nullable: false),
                    Description = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    CreatedBy = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    UpdatedBy = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Roles", x => x.Id);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    FirstName = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                    LastName = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                    EmailAddress = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false),
                    DisplayName = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true),
                    IsActive = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    Rating = table.Column<int>(type: "int", nullable: false, defaultValue: 1000),
                    AvatarVersion = table.Column<Guid>(type: "char(36)", nullable: true),
                    EmailVerifiedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    CreatedBy = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    UpdatedBy = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                    table.CheckConstraint("CK_User_Rating", "`Rating` >= 0");
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "TrainingSessions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    TrainingType = table.Column<int>(type: "int", nullable: false),
                    StartedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    FinishedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    Notes = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    CreatedBy = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    UpdatedBy = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrainingSessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TrainingSessions_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "UserAvatars",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    ContentType = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false),
                    Data = table.Column<byte[]>(type: "mediumblob", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    CreatedBy = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    UpdatedBy = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserAvatars", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserAvatars_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "UserCredentials",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    PasswordHash = table.Column<string>(type: "varchar(512)", maxLength: 512, nullable: false),
                    PasswordChangedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    MustChangePassword = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    RefreshTokenHash = table.Column<string>(type: "varchar(512)", maxLength: 512, nullable: true),
                    RefreshTokenExpiresAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    RefreshTokenRevokedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    CreatedBy = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    UpdatedBy = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserCredentials", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserCredentials_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "UserRoles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    RoleId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    CreatedBy = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    UpdatedBy = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserRoles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserRoles_Roles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "Roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UserRoles_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "AroundTheClockTrainings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    TrainingSessionId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    CreatedBy = table.Column<string>(type: "longtext", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    UpdatedBy = table.Column<string>(type: "longtext", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AroundTheClockTrainings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AroundTheClockTrainings_TrainingSessions_TrainingSessionId",
                        column: x => x.TrainingSessionId,
                        principalTable: "TrainingSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "CheckoutTrainings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    TargetScore = table.Column<int>(type: "int", nullable: false),
                    TrainingSessionId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    CreatedBy = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    UpdatedBy = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CheckoutTrainings", x => x.Id);
                    table.CheckConstraint("CK_CheckoutTraining_TargetScore", "`TargetScore` BETWEEN 2 AND 170");
                    table.ForeignKey(
                        name: "FK_CheckoutTrainings_TrainingSessions_TrainingSessionId",
                        column: x => x.TrainingSessionId,
                        principalTable: "TrainingSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "SegmentTrainings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    TargetSegment = table.Column<int>(type: "int", nullable: false),
                    TargetMultiplier = table.Column<int>(type: "int", nullable: false),
                    NumberOfVisits = table.Column<int>(type: "int", nullable: false),
                    TrainingSessionId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    CreatedBy = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    UpdatedBy = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SegmentTrainings", x => x.Id);
                    table.CheckConstraint("CK_SegmentTraining_NumberOfVisits", "`NumberOfVisits` > 0");
                    table.CheckConstraint("CK_SegmentTraining_Target", "((`TargetSegment` BETWEEN 1 AND 20 AND `TargetMultiplier` BETWEEN 1 AND 3) OR (`TargetSegment` = 25 AND `TargetMultiplier` BETWEEN 1 AND 2))");
                    table.ForeignKey(
                        name: "FK_SegmentTrainings_TrainingSessions_TrainingSessionId",
                        column: x => x.TrainingSessionId,
                        principalTable: "TrainingSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "TrainingGames",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    StartingScore = table.Column<int>(type: "int", nullable: false),
                    InMode = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    OutMode = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    NumberOfLegs = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    TrainingSessionId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    CreatedBy = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    UpdatedBy = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrainingGames", x => x.Id);
                    table.CheckConstraint("CK_TrainingGame_NumberOfLegs", "`NumberOfLegs` >= 1");
                    table.CheckConstraint("CK_TrainingGame_StartingScore", "`StartingScore` IN (301, 501)");
                    table.ForeignKey(
                        name: "FK_TrainingGames_TrainingSessions_TrainingSessionId",
                        column: x => x.TrainingSessionId,
                        principalTable: "TrainingSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "AroundTheClockTrainingVisits",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    VisitNumber = table.Column<int>(type: "int", nullable: false),
                    AroundTheClockTrainingId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    CreatedBy = table.Column<string>(type: "longtext", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    UpdatedBy = table.Column<string>(type: "longtext", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AroundTheClockTrainingVisits", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AroundTheClockTrainingVisits_AroundTheClockTrainings_AroundT~",
                        column: x => x.AroundTheClockTrainingId,
                        principalTable: "AroundTheClockTrainings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "CheckoutTrainingAttempts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    AttemptNumber = table.Column<int>(type: "int", nullable: false),
                    IsCheckout = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    CheckoutTrainingId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    CreatedBy = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    UpdatedBy = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CheckoutTrainingAttempts", x => x.Id);
                    table.CheckConstraint("CK_CheckoutTrainingAttempt_AttemptNumber", "`AttemptNumber` > 0");
                    table.ForeignKey(
                        name: "FK_CheckoutTrainingAttempts_CheckoutTrainings_CheckoutTrainingId",
                        column: x => x.CheckoutTrainingId,
                        principalTable: "CheckoutTrainings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "SegmentTrainingThrows",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    VisitNumber = table.Column<int>(type: "int", nullable: false),
                    ThrowNumber = table.Column<int>(type: "int", nullable: false),
                    Segment = table.Column<int>(type: "int", nullable: false),
                    Multiplier = table.Column<int>(type: "int", nullable: false),
                    Score = table.Column<int>(type: "int", nullable: false),
                    IsHit = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    SegmentTrainingId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    CreatedBy = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    UpdatedBy = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SegmentTrainingThrows", x => x.Id);
                    table.CheckConstraint("CK_SegmentTrainingThrow_Score", "`Score` = `Segment` * `Multiplier`");
                    table.CheckConstraint("CK_SegmentTrainingThrow_SegmentMultiplier", "((`Segment` BETWEEN 1 AND 20 AND `Multiplier` BETWEEN 1 AND 3) OR (`Segment` = 25 AND `Multiplier` BETWEEN 1 AND 2) OR (`Segment` = 0 AND `Multiplier` = 0))");
                    table.CheckConstraint("CK_SegmentTrainingThrow_ThrowNumber", "`ThrowNumber` BETWEEN 1 AND 3");
                    table.CheckConstraint("CK_SegmentTrainingThrow_VisitNumber", "`VisitNumber` > 0");
                    table.ForeignKey(
                        name: "FK_SegmentTrainingThrows_SegmentTrainings_SegmentTrainingId",
                        column: x => x.SegmentTrainingId,
                        principalTable: "SegmentTrainings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "TrainingGameVisits",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    VisitNumber = table.Column<int>(type: "int", nullable: false),
                    LegNumber = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    ScoreBefore = table.Column<int>(type: "int", nullable: false),
                    ScoreAfter = table.Column<int>(type: "int", nullable: false),
                    IsBust = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    TrainingGameId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    CreatedBy = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    UpdatedBy = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrainingGameVisits", x => x.Id);
                    table.CheckConstraint("CK_TrainingGameVisit_LegNumber", "`LegNumber` > 0");
                    table.CheckConstraint("CK_TrainingGameVisit_ScoreAfter", "`ScoreAfter` BETWEEN 0 AND `ScoreBefore` AND `ScoreBefore` - `ScoreAfter` <= 180 AND (`IsBust` = 0 OR `ScoreAfter` = `ScoreBefore`)");
                    table.CheckConstraint("CK_TrainingGameVisit_ScoreBefore", "`ScoreBefore` BETWEEN 1 AND 501");
                    table.CheckConstraint("CK_TrainingGameVisit_VisitNumber", "`VisitNumber` > 0");
                    table.ForeignKey(
                        name: "FK_TrainingGameVisits_TrainingGames_TrainingGameId",
                        column: x => x.TrainingGameId,
                        principalTable: "TrainingGames",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "AroundTheClockTrainingThrows",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    ThrowNumber = table.Column<int>(type: "int", nullable: false),
                    Segment = table.Column<int>(type: "int", nullable: false),
                    Multiplier = table.Column<int>(type: "int", nullable: false),
                    Score = table.Column<int>(type: "int", nullable: false),
                    AroundTheClockTrainingVisitId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    CreatedBy = table.Column<string>(type: "longtext", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    UpdatedBy = table.Column<string>(type: "longtext", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AroundTheClockTrainingThrows", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AroundTheClockTrainingThrows_AroundTheClockTrainingVisits_Ar~",
                        column: x => x.AroundTheClockTrainingVisitId,
                        principalTable: "AroundTheClockTrainingVisits",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "CheckoutTrainingThrows",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    ThrowNumber = table.Column<int>(type: "int", nullable: false),
                    Segment = table.Column<int>(type: "int", nullable: false),
                    Multiplier = table.Column<int>(type: "int", nullable: false),
                    Score = table.Column<int>(type: "int", nullable: false),
                    CheckoutTrainingAttemptId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    CreatedBy = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    UpdatedBy = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CheckoutTrainingThrows", x => x.Id);
                    table.CheckConstraint("CK_CheckoutTrainingThrow_Score", "`Score` = `Segment` * `Multiplier`");
                    table.CheckConstraint("CK_CheckoutTrainingThrow_SegmentMultiplier", "((`Segment` BETWEEN 1 AND 20 AND `Multiplier` BETWEEN 1 AND 3) OR (`Segment` = 25 AND `Multiplier` BETWEEN 1 AND 2) OR (`Segment` = 0 AND `Multiplier` = 0))");
                    table.CheckConstraint("CK_CheckoutTrainingThrow_ThrowNumber", "`ThrowNumber` BETWEEN 1 AND 3");
                    table.ForeignKey(
                        name: "FK_CheckoutTrainingThrows_CheckoutTrainingAttempts_CheckoutTrai~",
                        column: x => x.CheckoutTrainingAttemptId,
                        principalTable: "CheckoutTrainingAttempts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "TrainingGameThrows",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    ThrowNumber = table.Column<int>(type: "int", nullable: false),
                    Segment = table.Column<int>(type: "int", nullable: false),
                    Multiplier = table.Column<int>(type: "int", nullable: false),
                    Score = table.Column<int>(type: "int", nullable: false),
                    TrainingGameVisitId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    CreatedBy = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    UpdatedBy = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrainingGameThrows", x => x.Id);
                    table.CheckConstraint("CK_TrainingGameThrow_Score", "`Score` = `Segment` * `Multiplier`");
                    table.CheckConstraint("CK_TrainingGameThrow_SegmentMultiplier", "((`Segment` BETWEEN 1 AND 20 AND `Multiplier` BETWEEN 1 AND 3) OR (`Segment` = 25 AND `Multiplier` BETWEEN 1 AND 2) OR (`Segment` = 0 AND `Multiplier` = 0))");
                    table.CheckConstraint("CK_TrainingGameThrow_ThrowNumber", "`ThrowNumber` BETWEEN 1 AND 3");
                    table.ForeignKey(
                        name: "FK_TrainingGameThrows_TrainingGameVisits_TrainingGameVisitId",
                        column: x => x.TrainingGameVisitId,
                        principalTable: "TrainingGameVisits",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
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
                    DisplayName = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                    XpGained = table.Column<long>(type: "bigint", nullable: true),
                    DayForm = table.Column<float>(type: "float", nullable: true),
                    AverageInGame = table.Column<float>(type: "float", nullable: true),
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
                    table.CheckConstraint("CK_DartBotGamePlayer_PlayerType", "(`PlayerType` = 2 AND `DartBotId` IS NOT NULL AND `ProfileVersionId` IS NOT NULL AND `UserId` IS NULL) OR (`PlayerType` = 1 AND `DartBotId` IS NULL AND `ProfileVersionId` IS NULL AND `XpGained` IS NULL)");
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
                    InMode = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    OutMode = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
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
                    table.CheckConstraint("CK_DartBotGameVisit_OpponentScoreBefore", "`OpponentScoreBefore` BETWEEN 1 AND 501");
                    table.CheckConstraint("CK_DartBotGameVisit_ScoreAfter", "`ScoreAfter` BETWEEN 0 AND `ScoreBefore` AND `ScoreBefore` - `ScoreAfter` <= 180 AND (`IsBust` = 0 OR `ScoreAfter` = `ScoreBefore`)");
                    table.CheckConstraint("CK_DartBotGameVisit_ScoreBefore", "`ScoreBefore` BETWEEN 1 AND 501");
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
                    BaseAverage = table.Column<float>(type: "float", nullable: true),
                    SigmaMm = table.Column<float>(type: "float", nullable: true),
                    VerticalRatio = table.Column<float>(type: "float", nullable: false),
                    BiasXMm = table.Column<float>(type: "float", nullable: false),
                    BiasYMm = table.Column<float>(type: "float", nullable: false),
                    BadDartRate = table.Column<float>(type: "float", nullable: false),
                    Nervousness = table.Column<float>(type: "float", nullable: false),
                    ChangeReason = table.Column<int>(type: "int", nullable: false),
                    ReasonText = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: true),
                    PreviousVersionId = table.Column<int>(type: "int", nullable: true),
                    TriggeringGameId = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    CreatedBy = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    UpdatedBy = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DartBotProfileVersions", x => x.Id);
                    table.UniqueConstraint("AK_DartBotProfileVersions_DartBotId_Id", x => new { x.DartBotId, x.Id });
                    table.CheckConstraint("CK_DartBotProfileVersion_BadDartRate", "`BadDartRate` BETWEEN 0 AND 1");
                    table.CheckConstraint("CK_DartBotProfileVersion_GameLearning", "`ChangeReason` <> 2 OR `TriggeringGameId` IS NOT NULL");
                    table.CheckConstraint("CK_DartBotProfileVersion_Nervousness", "`Nervousness` BETWEEN 0 AND 1");
                    table.CheckConstraint("CK_DartBotProfileVersion_PressureResistance", "`PressureResistance` BETWEEN 0 AND 1");
                    table.CheckConstraint("CK_DartBotProfileVersion_RiskTaking", "`RiskTaking` BETWEEN 0 AND 1");
                    table.CheckConstraint("CK_DartBotProfileVersion_SigmaMm", "`SigmaMm` IS NULL OR `SigmaMm` > 0");
                    table.CheckConstraint("CK_DartBotProfileVersion_TacticalIntelligence", "`TacticalIntelligence` BETWEEN 0 AND 1");
                    table.CheckConstraint("CK_DartBotProfileVersion_ThrowConsistency", "`ThrowConsistency` BETWEEN 0 AND 1");
                    table.CheckConstraint("CK_DartBotProfileVersion_ThrowPrecision", "`ThrowPrecision` BETWEEN 0 AND 1");
                    table.CheckConstraint("CK_DartBotProfileVersion_VersionNumber", "`VersionNumber` >= 1");
                    table.CheckConstraint("CK_DartBotProfileVersion_VerticalRatio", "`VerticalRatio` > 0");
                    table.ForeignKey(
                        name: "FK_DartBotProfileVersions_DartBotGames_TriggeringGameId",
                        column: x => x.TriggeringGameId,
                        principalTable: "DartBotGames",
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
                    table.CheckConstraint("CK_DartBotGameThrow_ScoreBeforeThrow", "`ScoreBeforeThrow` BETWEEN 1 AND 501");
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
                    AwayProfileVersionId = table.Column<int>(type: "int", nullable: true),
                    HomeLegs = table.Column<int>(type: "int", nullable: false),
                    AwayLegs = table.Column<int>(type: "int", nullable: false),
                    WinnerParticipantId = table.Column<int>(type: "int", nullable: true),
                    NextMatchId = table.Column<int>(type: "int", nullable: true),
                    NextMatchSlot = table.Column<int>(type: "int", nullable: true),
                    ConductedByUserId = table.Column<int>(type: "int", nullable: true),
                    HomeRatingBefore = table.Column<int>(type: "int", nullable: true),
                    HomeRatingAfter = table.Column<int>(type: "int", nullable: true),
                    AwayRatingBefore = table.Column<int>(type: "int", nullable: true),
                    AwayRatingAfter = table.Column<int>(type: "int", nullable: true),
                    HomeXpGained = table.Column<long>(type: "bigint", nullable: true),
                    AwayXpGained = table.Column<long>(type: "bigint", nullable: true),
                    LearningProcessedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
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
                    table.CheckConstraint("CK_TournamentMatch_Learning", "`LearningProcessedAt` IS NULL OR `Status` = 3");
                    table.CheckConstraint("CK_TournamentMatch_Legs", "`HomeLegs` >= 0 AND `AwayLegs` >= 0");
                    table.CheckConstraint("CK_TournamentMatch_NextMatch", "(`NextMatchId` IS NULL AND `NextMatchSlot` IS NULL) OR (`NextMatchId` IS NOT NULL AND `NextMatchSlot` IN (1, 2))");
                    table.CheckConstraint("CK_TournamentMatch_Sides", "`HomeParticipantId` IS NULL OR `AwayParticipantId` IS NULL OR `HomeParticipantId` <> `AwayParticipantId`");
                    table.CheckConstraint("CK_TournamentMatch_Status", "(`Status` = 1) OR (`Status` IN (2, 3) AND `HomeParticipantId` IS NOT NULL AND `AwayParticipantId` IS NOT NULL AND `StartedAt` IS NOT NULL) OR (`Status` = 4 AND (`HomeParticipantId` IS NULL) <> (`AwayParticipantId` IS NULL))");
                    table.CheckConstraint("CK_TournamentMatch_Winner", "(`Status` IN (3, 4) AND `WinnerParticipantId` IS NOT NULL) OR (`Status` IN (1, 2) AND `WinnerParticipantId` IS NULL)");
                    table.CheckConstraint("CK_TournamentMatch_Xp", "(`HomeXpGained` IS NULL OR `HomeXpGained` >= 0) AND (`AwayXpGained` IS NULL OR `AwayXpGained` >= 0)");
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

            migrationBuilder.InsertData(
                table: "Roles",
                columns: new[] { "Id", "CreatedAt", "CreatedBy", "Description", "UpdatedAt", "UpdatedBy", "UserRole" },
                values: new object[,]
                {
                    { 1, new DateTime(2026, 11, 11, 0, 0, 0, 0, DateTimeKind.Utc), "System", "Administrator role with full access", null, null, 0 },
                    { 2, new DateTime(2026, 11, 11, 0, 0, 0, 0, DateTimeKind.Utc), "System", "Game Master role with limited access", null, null, 2 },
                    { 3, new DateTime(2026, 11, 11, 0, 0, 0, 0, DateTimeKind.Utc), "System", "Player role with basic access", null, null, 1 }
                });

            migrationBuilder.CreateIndex(
                name: "IX_AroundTheClockTrainings_TrainingSessionId",
                table: "AroundTheClockTrainings",
                column: "TrainingSessionId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AroundTheClockTrainingThrows_AroundTheClockTrainingVisitId",
                table: "AroundTheClockTrainingThrows",
                column: "AroundTheClockTrainingVisitId");

            migrationBuilder.CreateIndex(
                name: "IX_AroundTheClockTrainingVisits_AroundTheClockTrainingId",
                table: "AroundTheClockTrainingVisits",
                column: "AroundTheClockTrainingId");

            migrationBuilder.CreateIndex(
                name: "IX_CheckoutTrainingAttempts_CheckoutTrainingId_AttemptNumber",
                table: "CheckoutTrainingAttempts",
                columns: new[] { "CheckoutTrainingId", "AttemptNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CheckoutTrainings_TrainingSessionId",
                table: "CheckoutTrainings",
                column: "TrainingSessionId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CheckoutTrainingThrows_CheckoutTrainingAttemptId_ThrowNumber",
                table: "CheckoutTrainingThrows",
                columns: new[] { "CheckoutTrainingAttemptId", "ThrowNumber" },
                unique: true);

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
                name: "IX_DartBotProfileVersions_DartBotId_VersionNumber",
                table: "DartBotProfileVersions",
                columns: new[] { "DartBotId", "VersionNumber" },
                unique: true);

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
                name: "IX_LogMessages_CreatedAt",
                table: "LogMessages",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_LogMessages_LogMessageType",
                table: "LogMessages",
                column: "LogMessageType");

            migrationBuilder.CreateIndex(
                name: "IX_Roles_UserRole",
                table: "Roles",
                column: "UserRole",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SegmentTrainings_TrainingSessionId",
                table: "SegmentTrainings",
                column: "TrainingSessionId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SegmentTrainingThrows_SegmentTrainingId_VisitNumber_ThrowNum~",
                table: "SegmentTrainingThrows",
                columns: new[] { "SegmentTrainingId", "VisitNumber", "ThrowNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TournamentMatches_AwayParticipantId",
                table: "TournamentMatches",
                column: "AwayParticipantId");

            migrationBuilder.CreateIndex(
                name: "IX_TournamentMatches_AwayProfileVersionId",
                table: "TournamentMatches",
                column: "AwayProfileVersionId");

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

            migrationBuilder.CreateIndex(
                name: "IX_TrainingGames_TrainingSessionId",
                table: "TrainingGames",
                column: "TrainingSessionId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TrainingGameThrows_TrainingGameVisitId_ThrowNumber",
                table: "TrainingGameThrows",
                columns: new[] { "TrainingGameVisitId", "ThrowNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TrainingGameVisits_TrainingGameId_VisitNumber",
                table: "TrainingGameVisits",
                columns: new[] { "TrainingGameId", "VisitNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TrainingSessions_UserId_StartedAt",
                table: "TrainingSessions",
                columns: new[] { "UserId", "StartedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_UserAvatars_UserId",
                table: "UserAvatars",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserCredentials_RefreshTokenHash",
                table: "UserCredentials",
                column: "RefreshTokenHash");

            migrationBuilder.CreateIndex(
                name: "IX_UserCredentials_UserId",
                table: "UserCredentials",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserRoles_RoleId",
                table: "UserRoles",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_UserRoles_UserId_RoleId",
                table: "UserRoles",
                columns: new[] { "UserId", "RoleId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_EmailAddress",
                table: "Users",
                column: "EmailAddress",
                unique: true);

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
                name: "FK_DartBotGamePlayers_DartBotProfileVersions_DartBotId_ProfileV~",
                table: "DartBotGamePlayers",
                columns: new[] { "DartBotId", "ProfileVersionId" },
                principalTable: "DartBotProfileVersions",
                principalColumns: new[] { "DartBotId", "Id" },
                onDelete: ReferentialAction.Restrict);

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
                name: "FK_DartBotGames_DartBotGamePlayers_WinnerPlayerId",
                table: "DartBotGames");

            migrationBuilder.DropForeignKey(
                name: "FK_TournamentParticipants_DartBots_DartBotId",
                table: "TournamentParticipants");

            migrationBuilder.DropForeignKey(
                name: "FK_TournamentParticipants_Users_UserId",
                table: "TournamentParticipants");

            migrationBuilder.DropForeignKey(
                name: "FK_Tournaments_Users_OwnerUserId",
                table: "Tournaments");

            migrationBuilder.DropForeignKey(
                name: "FK_Tournaments_TournamentParticipants_WinnerParticipantId",
                table: "Tournaments");

            migrationBuilder.DropTable(
                name: "AroundTheClockTrainingThrows");

            migrationBuilder.DropTable(
                name: "CheckoutTrainingThrows");

            migrationBuilder.DropTable(
                name: "DartBotGameThrows");

            migrationBuilder.DropTable(
                name: "LogMessages");

            migrationBuilder.DropTable(
                name: "SegmentTrainingThrows");

            migrationBuilder.DropTable(
                name: "TournamentMatchThrows");

            migrationBuilder.DropTable(
                name: "TrainingGameThrows");

            migrationBuilder.DropTable(
                name: "UserAvatars");

            migrationBuilder.DropTable(
                name: "UserCredentials");

            migrationBuilder.DropTable(
                name: "UserRoles");

            migrationBuilder.DropTable(
                name: "AroundTheClockTrainingVisits");

            migrationBuilder.DropTable(
                name: "CheckoutTrainingAttempts");

            migrationBuilder.DropTable(
                name: "DartBotGameVisits");

            migrationBuilder.DropTable(
                name: "SegmentTrainings");

            migrationBuilder.DropTable(
                name: "TournamentMatchVisits");

            migrationBuilder.DropTable(
                name: "TrainingGameVisits");

            migrationBuilder.DropTable(
                name: "Roles");

            migrationBuilder.DropTable(
                name: "AroundTheClockTrainings");

            migrationBuilder.DropTable(
                name: "CheckoutTrainings");

            migrationBuilder.DropTable(
                name: "DartBotGameLegs");

            migrationBuilder.DropTable(
                name: "TournamentMatchLegs");

            migrationBuilder.DropTable(
                name: "TrainingGames");

            migrationBuilder.DropTable(
                name: "TournamentMatches");

            migrationBuilder.DropTable(
                name: "TrainingSessions");

            migrationBuilder.DropTable(
                name: "TournamentRounds");

            migrationBuilder.DropTable(
                name: "DartBotGamePlayers");

            migrationBuilder.DropTable(
                name: "DartBotProfileVersions");

            migrationBuilder.DropTable(
                name: "DartBotGames");

            migrationBuilder.DropTable(
                name: "DartBots");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "TournamentParticipants");

            migrationBuilder.DropTable(
                name: "Tournaments");
        }
    }
}
