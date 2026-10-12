using Microsoft.EntityFrameworkCore;

namespace VolksbankTracker.Core.Data;

/// <summary>
/// One-shot copy of every row from the SQLite database into an empty Postgres database,
/// keeping primary keys. AppSettings values are copied verbatim, so the Data Protection
/// encrypted FinTS credentials stay readable as long as the same key ring is used.
/// </summary>
public sealed class SqliteToPostgresCopier(AppDbContext source, AppDbContext target)
{
    private static readonly string[] IdentityTables =
        ["Categories", "MerchantCategoryMaps", "Transactions", "SyncLogs"];

    public async Task<TableCounts> CopyAsync(CancellationToken ct = default)
    {
        if (!source.Database.IsSqlite() || !target.Database.IsNpgsql())
            throw new InvalidOperationException("Source must be SQLite and target must be Postgres.");

        if ((await source.Database.GetPendingMigrationsAsync(ct)).Any())
            throw new InvalidOperationException(
                "The SQLite database has pending migrations. Start the app once on SQLite to apply them, then copy.");

        await target.Database.MigrateAsync(ct);
        await EnsureTargetHoldsOnlySeedDataAsync(ct);

        var categories = await source.Categories.AsNoTracking().ToListAsync(ct);
        var maps = await source.MerchantCategoryMaps.AsNoTracking().ToListAsync(ct);
        var transactions = await source.Transactions.AsNoTracking().ToListAsync(ct);
        var syncLogs = await source.SyncLogs.AsNoTracking().ToListAsync(ct);
        var settings = await source.AppSettings.AsNoTracking().ToListAsync(ct);

        await using var tx = await target.Database.BeginTransactionAsync(ct);

        // The seeded categories may have been renamed or deleted in SQLite; its rows win.
        await target.Categories.ExecuteDeleteAsync(ct);
        target.Categories.AddRange(categories);
        target.MerchantCategoryMaps.AddRange(maps);
        target.Transactions.AddRange(transactions);
        target.SyncLogs.AddRange(syncLogs);
        target.AppSettings.AddRange(settings);
        await target.SaveChangesAsync(ct);

        // Explicit keys bypass the identity sequences; move them past the copied rows.
        // Table names come from the constant list above, never from input.
        foreach (var table in IdentityTables)
        {
            var qualified = $"{AppDbContext.PostgresSchema}.\"{table}\"";
            var sql = $"""SELECT setval(pg_get_serial_sequence('{qualified}', 'Id'), COALESCE(MAX("Id"), 0) + 1, false) FROM {qualified}""";
            await target.Database.ExecuteSqlRawAsync(sql, ct);
        }

        var expected = await TableCounts.OfAsync(source, ct);
        var copied = await TableCounts.OfAsync(target, ct);
        if (copied != expected)
            throw new InvalidOperationException($"Row counts differ after copy. SQLite: {expected}, Postgres: {copied}.");

        await tx.CommitAsync(ct);
        return copied;
    }

    private async Task EnsureTargetHoldsOnlySeedDataAsync(CancellationToken ct)
    {
        var counts = await TableCounts.OfAsync(target, ct);
        if (counts != new TableCounts(CategorySeeder.Categories.Length, 0, 0, 0, 0))
            throw new InvalidOperationException(
                $"The Postgres database already holds data ({counts}). The copy only runs into an empty database.");
    }
}
