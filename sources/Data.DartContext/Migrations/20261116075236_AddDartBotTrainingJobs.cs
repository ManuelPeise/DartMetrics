using System;
using Microsoft.EntityFrameworkCore.Migrations;
using MySql.EntityFrameworkCore.Metadata;

#nullable disable

namespace Data.DartContext.Migrations
{
    /// <inheritdoc />
    public partial class AddDartBotTrainingJobs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DartBotTrainingJobs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    Preset = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Phase = table.Column<int>(type: "int", nullable: true),
                    ProgressPercent = table.Column<int>(type: "int", nullable: false),
                    ProgressText = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true),
                    Situations = table.Column<int>(type: "int", nullable: false),
                    RolloutsPerCandidate = table.Column<int>(type: "int", nullable: false),
                    CompareLegs = table.Column<int>(type: "int", nullable: false),
                    Seed = table.Column<int>(type: "int", nullable: false),
                    RequestedBy = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                    CancelRequested = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    StartedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    FinishedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    HeartbeatAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    ErrorMessage = table.Column<string>(type: "varchar(2000)", maxLength: 2000, nullable: true),
                    DatasetId = table.Column<int>(type: "int", nullable: true),
                    ModelId = table.Column<int>(type: "int", nullable: true),
                    ComparisonPassed = table.Column<bool>(type: "tinyint(1)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    CreatedBy = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    UpdatedBy = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DartBotTrainingJobs", x => x.Id);
                    table.CheckConstraint("CK_DartBotTrainingJob_Progress", "`ProgressPercent` BETWEEN 0 AND 100");
                    table.ForeignKey(
                        name: "FK_DartBotTrainingJobs_DartBotModels_ModelId",
                        column: x => x.ModelId,
                        principalTable: "DartBotModels",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DartBotTrainingJobs_DartBotTrainingDatasets_DatasetId",
                        column: x => x.DatasetId,
                        principalTable: "DartBotTrainingDatasets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_DartBotTrainingJobs_DatasetId",
                table: "DartBotTrainingJobs",
                column: "DatasetId");

            migrationBuilder.CreateIndex(
                name: "IX_DartBotTrainingJobs_ModelId",
                table: "DartBotTrainingJobs",
                column: "ModelId");

            migrationBuilder.CreateIndex(
                name: "IX_DartBotTrainingJobs_Status",
                table: "DartBotTrainingJobs",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DartBotTrainingJobs");
        }
    }
}
