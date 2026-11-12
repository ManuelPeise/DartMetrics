using System;
using Microsoft.EntityFrameworkCore.Migrations;
using MySql.EntityFrameworkCore.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Data.DartContext.Migrations
{
    /// <inheritdoc />
    public partial class InitializeDatabase : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
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
                    EmailVerifiedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    CreatedBy = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    UpdatedBy = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
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
                name: "UserCredentials",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    PasswordHash = table.Column<string>(type: "varchar(512)", maxLength: 512, nullable: false),
                    PasswordChangedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
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
                    TrainingSessionId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    CreatedBy = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    UpdatedBy = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrainingGames", x => x.Id);
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
                    table.CheckConstraint("CK_TrainingGameVisit_ScoreAfter", "`ScoreAfter` BETWEEN 0 AND `ScoreBefore` AND `ScoreBefore` - `ScoreAfter` <= 180 AND (`IsBust` = 0 OR `ScoreAfter` = `ScoreBefore`)");
                    table.CheckConstraint("CK_TrainingGameVisit_ScoreBefore", "`ScoreBefore` BETWEEN 2 AND 501");
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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CheckoutTrainingThrows");

            migrationBuilder.DropTable(
                name: "LogMessages");

            migrationBuilder.DropTable(
                name: "SegmentTrainingThrows");

            migrationBuilder.DropTable(
                name: "TrainingGameThrows");

            migrationBuilder.DropTable(
                name: "UserCredentials");

            migrationBuilder.DropTable(
                name: "UserRoles");

            migrationBuilder.DropTable(
                name: "CheckoutTrainingAttempts");

            migrationBuilder.DropTable(
                name: "SegmentTrainings");

            migrationBuilder.DropTable(
                name: "TrainingGameVisits");

            migrationBuilder.DropTable(
                name: "Roles");

            migrationBuilder.DropTable(
                name: "CheckoutTrainings");

            migrationBuilder.DropTable(
                name: "TrainingGames");

            migrationBuilder.DropTable(
                name: "TrainingSessions");

            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}
