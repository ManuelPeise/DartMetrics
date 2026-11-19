using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Data.DartContext.Migrations
{
    /// <inheritdoc />
    public partial class AroundTheClockConstraints : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // MySQL: Ein Fremdschlüssel braucht immer einen Index. Deshalb zuerst die neuen eindeutigen Indizes anlegen
            // (sie beginnen mit der Fremdschlüssel-Spalte), erst danach die alten einspaltigen löschen.
            migrationBuilder.CreateIndex(
                name: "IX_AroundTheClockTrainingVisits_AroundTheClockTrainingId_VisitN~",
                table: "AroundTheClockTrainingVisits",
                columns: new[] { "AroundTheClockTrainingId", "VisitNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AroundTheClockTrainingThrows_AroundTheClockTrainingVisitId_T~",
                table: "AroundTheClockTrainingThrows",
                columns: new[] { "AroundTheClockTrainingVisitId", "ThrowNumber" },
                unique: true);

            migrationBuilder.DropIndex(
                name: "IX_AroundTheClockTrainingVisits_AroundTheClockTrainingId",
                table: "AroundTheClockTrainingVisits");

            migrationBuilder.DropIndex(
                name: "IX_AroundTheClockTrainingThrows_AroundTheClockTrainingVisitId",
                table: "AroundTheClockTrainingThrows");

            migrationBuilder.AlterColumn<string>(
                name: "UpdatedBy",
                table: "AroundTheClockTrainingVisits",
                type: "varchar(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "longtext",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "CreatedBy",
                table: "AroundTheClockTrainingVisits",
                type: "varchar(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "longtext");

            migrationBuilder.AlterColumn<string>(
                name: "UpdatedBy",
                table: "AroundTheClockTrainingThrows",
                type: "varchar(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "longtext",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "CreatedBy",
                table: "AroundTheClockTrainingThrows",
                type: "varchar(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "longtext");

            migrationBuilder.AlterColumn<string>(
                name: "UpdatedBy",
                table: "AroundTheClockTrainings",
                type: "varchar(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "longtext",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "CreatedBy",
                table: "AroundTheClockTrainings",
                type: "varchar(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "longtext");

            migrationBuilder.AddCheckConstraint(
                name: "CK_AroundTheClockTrainingVisit_VisitNumber",
                table: "AroundTheClockTrainingVisits",
                sql: "`VisitNumber` > 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_AroundTheClockTrainingThrow_Score",
                table: "AroundTheClockTrainingThrows",
                sql: "`Score` = `Segment` * `Multiplier`");

            migrationBuilder.AddCheckConstraint(
                name: "CK_AroundTheClockTrainingThrow_SegmentMultiplier",
                table: "AroundTheClockTrainingThrows",
                sql: "((`Segment` BETWEEN 1 AND 20 AND `Multiplier` BETWEEN 1 AND 3) OR (`Segment` = 25 AND `Multiplier` BETWEEN 1 AND 2) OR (`Segment` = 0 AND `Multiplier` = 0))");

            migrationBuilder.AddCheckConstraint(
                name: "CK_AroundTheClockTrainingThrow_ThrowNumber",
                table: "AroundTheClockTrainingThrows",
                sql: "`ThrowNumber` BETWEEN 1 AND 3");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Umgekehrt wie Up: erst die einspaltigen Indizes wieder anlegen, dann die eindeutigen löschen.
            migrationBuilder.CreateIndex(
                name: "IX_AroundTheClockTrainingVisits_AroundTheClockTrainingId",
                table: "AroundTheClockTrainingVisits",
                column: "AroundTheClockTrainingId");

            migrationBuilder.CreateIndex(
                name: "IX_AroundTheClockTrainingThrows_AroundTheClockTrainingVisitId",
                table: "AroundTheClockTrainingThrows",
                column: "AroundTheClockTrainingVisitId");

            migrationBuilder.DropIndex(
                name: "IX_AroundTheClockTrainingVisits_AroundTheClockTrainingId_VisitN~",
                table: "AroundTheClockTrainingVisits");

            migrationBuilder.DropCheckConstraint(
                name: "CK_AroundTheClockTrainingVisit_VisitNumber",
                table: "AroundTheClockTrainingVisits");

            migrationBuilder.DropIndex(
                name: "IX_AroundTheClockTrainingThrows_AroundTheClockTrainingVisitId_T~",
                table: "AroundTheClockTrainingThrows");

            migrationBuilder.DropCheckConstraint(
                name: "CK_AroundTheClockTrainingThrow_Score",
                table: "AroundTheClockTrainingThrows");

            migrationBuilder.DropCheckConstraint(
                name: "CK_AroundTheClockTrainingThrow_SegmentMultiplier",
                table: "AroundTheClockTrainingThrows");

            migrationBuilder.DropCheckConstraint(
                name: "CK_AroundTheClockTrainingThrow_ThrowNumber",
                table: "AroundTheClockTrainingThrows");

            migrationBuilder.AlterColumn<string>(
                name: "UpdatedBy",
                table: "AroundTheClockTrainingVisits",
                type: "longtext",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(100)",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "CreatedBy",
                table: "AroundTheClockTrainingVisits",
                type: "longtext",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<string>(
                name: "UpdatedBy",
                table: "AroundTheClockTrainingThrows",
                type: "longtext",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(100)",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "CreatedBy",
                table: "AroundTheClockTrainingThrows",
                type: "longtext",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<string>(
                name: "UpdatedBy",
                table: "AroundTheClockTrainings",
                type: "longtext",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(100)",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "CreatedBy",
                table: "AroundTheClockTrainings",
                type: "longtext",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(100)",
                oldMaxLength: 100);
        }
    }
}
