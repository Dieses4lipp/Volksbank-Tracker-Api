using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace VolksbankTracker.Migrations.Postgres.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "volksbank");

            migrationBuilder.CreateTable(
                name: "AppSettings",
                schema: "volksbank",
                columns: table => new
                {
                    Key = table.Column<string>(type: "text", nullable: false),
                    Value = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppSettings", x => x.Key);
                });

            migrationBuilder.CreateTable(
                name: "Categories",
                schema: "volksbank",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Icon = table.Column<string>(type: "text", nullable: false),
                    Color = table.Column<string>(type: "text", nullable: false),
                    IsFallback = table.Column<bool>(type: "boolean", nullable: false),
                    IsSavings = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Categories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SyncLogs",
                schema: "volksbank",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    StartedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TransactionsFetched = table.Column<int>(type: "integer", nullable: false),
                    TransactionsNew = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    Error = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SyncLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MerchantCategoryMaps",
                schema: "volksbank",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    MatchKey = table.Column<string>(type: "text", nullable: false),
                    CategoryId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MerchantCategoryMaps", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MerchantCategoryMaps_Categories_CategoryId",
                        column: x => x.CategoryId,
                        principalSchema: "volksbank",
                        principalTable: "Categories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Transactions",
                schema: "volksbank",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Hash = table.Column<string>(type: "text", nullable: false),
                    BookingDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ValueDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric", nullable: false),
                    Currency = table.Column<string>(type: "text", nullable: false),
                    Purpose = table.Column<string>(type: "text", nullable: false),
                    CreditorName = table.Column<string>(type: "text", nullable: false),
                    CreditorIban = table.Column<string>(type: "text", nullable: false),
                    DebtorName = table.Column<string>(type: "text", nullable: false),
                    DebtorIban = table.Column<string>(type: "text", nullable: false),
                    CategoryId = table.Column<int>(type: "integer", nullable: true),
                    IsRecurring = table.Column<bool>(type: "boolean", nullable: false),
                    ImportedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Transactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Transactions_Categories_CategoryId",
                        column: x => x.CategoryId,
                        principalSchema: "volksbank",
                        principalTable: "Categories",
                        principalColumn: "Id");
                });

            migrationBuilder.InsertData(
                schema: "volksbank",
                table: "Categories",
                columns: new[] { "Id", "Color", "Icon", "IsFallback", "IsSavings", "Name" },
                values: new object[,]
                {
                    { 1, "#22c55e", "💰", false, false, "Einkommen" },
                    { 2, "#ef4444", "🏠", false, false, "Miete" },
                    { 3, "#f97316", "🛒", false, false, "Lebensmittel" },
                    { 4, "#3b82f6", "🚗", false, false, "Transport" },
                    { 5, "#8b5cf6", "🛡️", false, false, "Versicherungen" },
                    { 6, "#ec4899", "🎮", false, false, "Freizeit" },
                    { 7, "#14b8a6", "💊", false, false, "Gesundheit" },
                    { 8, "#6b7280", "📦", true, false, "Sonstiges" },
                    { 9, "#0ea5e9", "💵", false, true, "Sparen" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_MerchantCategoryMaps_CategoryId",
                schema: "volksbank",
                table: "MerchantCategoryMaps",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_MerchantCategoryMaps_MatchKey",
                schema: "volksbank",
                table: "MerchantCategoryMaps",
                column: "MatchKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_CategoryId",
                schema: "volksbank",
                table: "Transactions",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_Hash",
                schema: "volksbank",
                table: "Transactions",
                column: "Hash",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AppSettings",
                schema: "volksbank");

            migrationBuilder.DropTable(
                name: "MerchantCategoryMaps",
                schema: "volksbank");

            migrationBuilder.DropTable(
                name: "SyncLogs",
                schema: "volksbank");

            migrationBuilder.DropTable(
                name: "Transactions",
                schema: "volksbank");

            migrationBuilder.DropTable(
                name: "Categories",
                schema: "volksbank");
        }
    }
}
