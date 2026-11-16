using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Data.DartContext.Migrations
{
    /// <inheritdoc />
    public partial class AddTrainingJobPolicyModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PolicyModelId",
                table: "DartBotTrainingJobs",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_DartBotTrainingJobs_PolicyModelId",
                table: "DartBotTrainingJobs",
                column: "PolicyModelId");

            migrationBuilder.AddForeignKey(
                name: "FK_DartBotTrainingJobs_DartBotModels_PolicyModelId",
                table: "DartBotTrainingJobs",
                column: "PolicyModelId",
                principalTable: "DartBotModels",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DartBotTrainingJobs_DartBotModels_PolicyModelId",
                table: "DartBotTrainingJobs");

            migrationBuilder.DropIndex(
                name: "IX_DartBotTrainingJobs_PolicyModelId",
                table: "DartBotTrainingJobs");

            migrationBuilder.DropColumn(
                name: "PolicyModelId",
                table: "DartBotTrainingJobs");
        }
    }
}
