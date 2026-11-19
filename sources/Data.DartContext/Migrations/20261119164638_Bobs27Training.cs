using System;
using Microsoft.EntityFrameworkCore.Migrations;
using MySql.EntityFrameworkCore.Metadata;

#nullable disable

namespace Data.DartContext.Migrations
{
    /// <inheritdoc />
    public partial class Bobs27Training : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Bobs27Trainings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    IsBeginnerMode = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    TrainingSessionId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    CreatedBy = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    UpdatedBy = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Bobs27Trainings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Bobs27Trainings_TrainingSessions_TrainingSessionId",
                        column: x => x.TrainingSessionId,
                        principalTable: "TrainingSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Bobs27TrainingPlayers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    PlayerNumber = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "varchar(40)", maxLength: 40, nullable: false),
                    Bobs27TrainingId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    CreatedBy = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    UpdatedBy = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Bobs27TrainingPlayers", x => x.Id);
                    table.CheckConstraint("CK_Bobs27TrainingPlayer_PlayerNumber", "`PlayerNumber` BETWEEN 1 AND 8");
                    table.ForeignKey(
                        name: "FK_Bobs27TrainingPlayers_Bobs27Trainings_Bobs27TrainingId",
                        column: x => x.Bobs27TrainingId,
                        principalTable: "Bobs27Trainings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Bobs27TrainingTurns",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    TurnNumber = table.Column<int>(type: "int", nullable: false),
                    PlayerNumber = table.Column<int>(type: "int", nullable: false),
                    Round = table.Column<int>(type: "int", nullable: false),
                    Hits = table.Column<int>(type: "int", nullable: false),
                    ScoreBefore = table.Column<int>(type: "int", nullable: false),
                    ScoreAfter = table.Column<int>(type: "int", nullable: false),
                    IsBust = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    Bobs27TrainingId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    CreatedBy = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    UpdatedBy = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Bobs27TrainingTurns", x => x.Id);
                    table.CheckConstraint("CK_Bobs27TrainingTurn_Hits", "`Hits` BETWEEN 0 AND 3");
                    table.CheckConstraint("CK_Bobs27TrainingTurn_PlayerNumber", "`PlayerNumber` BETWEEN 1 AND 8");
                    table.CheckConstraint("CK_Bobs27TrainingTurn_Round", "`Round` BETWEEN 1 AND 21");
                    table.CheckConstraint("CK_Bobs27TrainingTurn_Score", "`ScoreAfter` = `ScoreBefore` + IF(`Hits` > 0, `Hits`, -1) * IF(`Round` = 21, 50, `Round` * 2)");
                    table.CheckConstraint("CK_Bobs27TrainingTurn_TurnNumber", "`TurnNumber` > 0");
                    table.ForeignKey(
                        name: "FK_Bobs27TrainingTurns_Bobs27Trainings_Bobs27TrainingId",
                        column: x => x.Bobs27TrainingId,
                        principalTable: "Bobs27Trainings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_Bobs27TrainingPlayers_Bobs27TrainingId_PlayerNumber",
                table: "Bobs27TrainingPlayers",
                columns: new[] { "Bobs27TrainingId", "PlayerNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Bobs27Trainings_TrainingSessionId",
                table: "Bobs27Trainings",
                column: "TrainingSessionId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Bobs27TrainingTurns_Bobs27TrainingId_PlayerNumber_Round",
                table: "Bobs27TrainingTurns",
                columns: new[] { "Bobs27TrainingId", "PlayerNumber", "Round" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Bobs27TrainingTurns_Bobs27TrainingId_TurnNumber",
                table: "Bobs27TrainingTurns",
                columns: new[] { "Bobs27TrainingId", "TurnNumber" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Bobs27TrainingPlayers");

            migrationBuilder.DropTable(
                name: "Bobs27TrainingTurns");

            migrationBuilder.DropTable(
                name: "Bobs27Trainings");
        }
    }
}
