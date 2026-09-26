using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VolksbankTracker.Core.Migrations
{
    /// <inheritdoc />
    public partial class UpdateSavingsCategoryIcon : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder) =>
            // Matched on the previous seed icon so a user-picked icon is not overwritten.
            migrationBuilder.Sql(
                """UPDATE "Categories" SET "Icon" = '💵' WHERE "IsSavings" = 1 AND "Icon" = '🐷';""");

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder) =>
            migrationBuilder.Sql(
                """UPDATE "Categories" SET "Icon" = '🐷' WHERE "IsSavings" = 1 AND "Icon" = '💵';""");
    }
}
