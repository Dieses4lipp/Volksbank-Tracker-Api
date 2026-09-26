using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VolksbankTracker.Core.Migrations
{
    /// <inheritdoc />
    public partial class AddSavingsCategory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsSavings",
                table: "Categories",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 1,
                column: "IsSavings",
                value: false);

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 2,
                column: "IsSavings",
                value: false);

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 3,
                column: "IsSavings",
                value: false);

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 4,
                column: "IsSavings",
                value: false);

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 5,
                column: "IsSavings",
                value: false);

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 6,
                column: "IsSavings",
                value: false);

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 7,
                column: "IsSavings",
                value: false);

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 8,
                column: "IsSavings",
                value: false);

            // Databases created before this migration may already hold a hand-made
            // "Sparen" category, possibly occupying Id 9, so the seed row cannot be
            // inserted blindly. Adopt an existing one, insert only when none exists.
            migrationBuilder.Sql(
                """
                UPDATE "Categories" SET "IsSavings" = 1 WHERE LOWER("Name") = 'sparen';

                INSERT INTO "Categories" ("Id", "Color", "Icon", "IsFallback", "IsSavings", "Name")
                SELECT 9, '#0ea5e9', '🐷', 0, 1, 'Sparen'
                WHERE NOT EXISTS (SELECT 1 FROM "Categories" WHERE "IsSavings" = 1)
                  AND NOT EXISTS (SELECT 1 FROM "Categories" WHERE "Id" = 9);

                INSERT INTO "Categories" ("Color", "Icon", "IsFallback", "IsSavings", "Name")
                SELECT '#0ea5e9', '🐷', 0, 1, 'Sparen'
                WHERE NOT EXISTS (SELECT 1 FROM "Categories" WHERE "IsSavings" = 1);
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Only removes the row this migration could have created: a seed-coloured,
            // unreferenced "Sparen". A hand-made one it merely adopted is left alone.
            migrationBuilder.Sql(
                """
                DELETE FROM "Categories"
                WHERE "IsSavings" = 1
                  AND "Icon" = '🐷' AND "Color" = '#0ea5e9'
                  AND NOT EXISTS (SELECT 1 FROM "Transactions" WHERE "CategoryId" = "Categories"."Id")
                  AND NOT EXISTS (SELECT 1 FROM "MerchantCategoryMaps" WHERE "CategoryId" = "Categories"."Id");
                """);

            migrationBuilder.DropColumn(
                name: "IsSavings",
                table: "Categories");
        }
    }
}
