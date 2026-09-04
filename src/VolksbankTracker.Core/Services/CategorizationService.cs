using Microsoft.EntityFrameworkCore;
using VolksbankTracker.Core.Data;

namespace VolksbankTracker.Core.Services;

public class CategorizationService(AppDbContext db)
{
    public async Task CategorizeAsync(Transaction t, Dictionary<string, int>? matchMap = null, List<Category>? categories = null)
    {
        if (t.CategoryId.HasValue) return;

        matchMap ??= await LoadMatchMapAsync();
        categories ??= await db.Categories.ToListAsync();

        var key = MatchKey(t);
        if (key is not null && matchMap.TryGetValue(key, out var categoryId))
        {
            t.CategoryId = categoryId;
            return;
        }

        t.CategoryId = categories.FirstOrDefault(c => c.IsFallback)?.Id;
    }

    public async Task LearnAsync(Transaction t, int categoryId)
    {
        var key = MatchKey(t);
        if (key is null) return;

        var existing = await db.MerchantCategoryMaps.FirstOrDefaultAsync(m => m.MatchKey == key);
        if (existing is not null)
            existing.CategoryId = categoryId;
        else
            db.MerchantCategoryMaps.Add(new MerchantCategoryMap { MatchKey = key, CategoryId = categoryId });
    }

    public async Task<int> RecategorizeAllAsync()
    {
        var transactions = await db.Transactions.ToListAsync();
        var categories = await db.Categories.ToListAsync();
        var matchMap = await LoadMatchMapAsync();
        foreach (var t in transactions)
        {
            t.CategoryId = null;
            await CategorizeAsync(t, matchMap, categories);
        }
        await db.SaveChangesAsync();
        return transactions.Count;
    }

    private async Task<Dictionary<string, int>> LoadMatchMapAsync() =>
        await db.MerchantCategoryMaps.ToDictionaryAsync(m => m.MatchKey, m => m.CategoryId);

    private static string? MatchKey(Transaction t)
    {
        var (iban, name) = t.Amount < 0
            ? (t.CreditorIban, t.CreditorName)
            : (t.DebtorIban, t.DebtorName);

        if (!string.IsNullOrWhiteSpace(iban)) return iban.Trim().ToUpperInvariant();
        if (!string.IsNullOrWhiteSpace(name)) return name.Trim().ToLowerInvariant();
        return null;
    }
}
