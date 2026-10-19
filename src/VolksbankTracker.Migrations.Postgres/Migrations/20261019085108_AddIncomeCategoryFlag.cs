using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VolksbankTracker.Migrations.Postgres.Migrations
{
    /// <inheritdoc />
    public partial class AddIncomeCategoryFlag : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsIncome",
                schema: "volksbank",
                table: "Categories",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.UpdateData(
                schema: "volksbank",
                table: "Categories",
                keyColumn: "Id",
                keyValue: 1,
                column: "IsIncome",
                value: true);

            migrationBuilder.UpdateData(
                schema: "volksbank",
                table: "Categories",
                keyColumn: "Id",
                keyValue: 2,
                column: "IsIncome",
                value: false);

            migrationBuilder.UpdateData(
                schema: "volksbank",
                table: "Categories",
                keyColumn: "Id",
                keyValue: 3,
                column: "IsIncome",
                value: false);

            migrationBuilder.UpdateData(
                schema: "volksbank",
                table: "Categories",
                keyColumn: "Id",
                keyValue: 4,
                column: "IsIncome",
                value: false);

            migrationBuilder.UpdateData(
                schema: "volksbank",
                table: "Categories",
                keyColumn: "Id",
                keyValue: 5,
                column: "IsIncome",
                value: false);

            migrationBuilder.UpdateData(
                schema: "volksbank",
                table: "Categories",
                keyColumn: "Id",
                keyValue: 6,
                column: "IsIncome",
                value: false);

            migrationBuilder.UpdateData(
                schema: "volksbank",
                table: "Categories",
                keyColumn: "Id",
                keyValue: 7,
                column: "IsIncome",
                value: false);

            migrationBuilder.UpdateData(
                schema: "volksbank",
                table: "Categories",
                keyColumn: "Id",
                keyValue: 8,
                column: "IsIncome",
                value: false);

            migrationBuilder.UpdateData(
                schema: "volksbank",
                table: "Categories",
                keyColumn: "Id",
                keyValue: 9,
                column: "IsIncome",
                value: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsIncome",
                schema: "volksbank",
                table: "Categories");
        }
    }
}
