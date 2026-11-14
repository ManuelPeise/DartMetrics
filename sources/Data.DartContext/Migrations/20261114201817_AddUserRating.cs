using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Data.DartContext.Migrations
{
    /// <inheritdoc />
    public partial class AddUserRating : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Rating",
                table: "Users",
                type: "int",
                nullable: false,
                defaultValue: 1000);

            migrationBuilder.AddCheckConstraint(
                name: "CK_User_Rating",
                table: "Users",
                sql: "`Rating` >= 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_User_Rating",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "Rating",
                table: "Users");
        }
    }
}
