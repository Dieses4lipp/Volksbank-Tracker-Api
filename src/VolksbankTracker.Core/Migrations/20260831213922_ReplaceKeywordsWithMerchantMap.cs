using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VolksbankTracker.Core.Migrations
{
    /// <inheritdoc />
    public partial class ReplaceKeywordsWithMerchantMap : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsIncome",
                table: "Categories");

            migrationBuilder.DropColumn(
                name: "Keywords",
                table: "Categories");

            migrationBuilder.CreateTable(
                name: "MerchantCategoryMaps",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    MatchKey = table.Column<string>(type: "TEXT", nullable: false),
                    CategoryId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MerchantCategoryMaps", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MerchantCategoryMaps_Categories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "Categories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MerchantCategoryMaps_CategoryId",
                table: "MerchantCategoryMaps",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_MerchantCategoryMaps_MatchKey",
                table: "MerchantCategoryMaps",
                column: "MatchKey",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MerchantCategoryMaps");

            migrationBuilder.AddColumn<bool>(
                name: "IsIncome",
                table: "Categories",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Keywords",
                table: "Categories",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "IsIncome", "Keywords" },
                values: new object[] { true, "gehalt|lohn|entgelt|gutschrift arbeitgeber" });

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "IsIncome", "Keywords" },
                values: new object[] { false, "miete|nebenkosten" });

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "IsIncome", "Keywords" },
                values: new object[] { false, "rewe|aldi|lidl|edeka|netto|kaufland|nah und gut|nah + gut|marktkauf|penny" });

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "IsIncome", "Keywords" },
                values: new object[] { false, "tank|aral|shell|db bahn|deutsche bahn|vgn|öpnv|parken" });

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 5,
                columns: new[] { "IsIncome", "Keywords" },
                values: new object[] { false, "versicherung|allianz|huk|aok|tkk|barmer|gkv" });

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 6,
                columns: new[] { "IsIncome", "Keywords" },
                values: new object[] { false, "netflix|spotify|steam|amazon prime|disney|kino|restaurant|lieferando" });

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 7,
                columns: new[] { "IsIncome", "Keywords" },
                values: new object[] { false, "apotheke|arzt|zahnarzt|xtra|fitnessstudio" });

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 8,
                columns: new[] { "IsIncome", "Keywords" },
                values: new object[] { false, "" });
        }
    }
}
