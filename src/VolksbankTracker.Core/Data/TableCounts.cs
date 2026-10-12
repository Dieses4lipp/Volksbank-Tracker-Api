using Microsoft.EntityFrameworkCore;

namespace VolksbankTracker.Core.Data;

public sealed record TableCounts(int Categories, int MerchantCategoryMaps, int Transactions, int SyncLogs, int AppSettings)
{
    public static async Task<TableCounts> OfAsync(AppDbContext db, CancellationToken ct = default) => new(
        await db.Categories.CountAsync(ct),
        await db.MerchantCategoryMaps.CountAsync(ct),
        await db.Transactions.CountAsync(ct),
        await db.SyncLogs.CountAsync(ct),
        await db.AppSettings.CountAsync(ct));
}
