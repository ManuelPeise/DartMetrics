using System;
using Microsoft.EntityFrameworkCore.Migrations;
using MySql.EntityFrameworkCore.Metadata;

#nullable disable

namespace Data.DartContext.Migrations
{
    /// <inheritdoc />
    public partial class AddAroundTheClockTraining : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AroundTheClockTrainingThrows");

            migrationBuilder.DropTable(
                name: "AroundTheClockTrainingVisits");

            migrationBuilder.DropTable(
                name: "AroundTheClockTrainings");
        }
    }
}
