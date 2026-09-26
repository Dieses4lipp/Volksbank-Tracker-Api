using Microsoft.EntityFrameworkCore;
using VolksbankTracker.Core.Data;

namespace VolksbankTracker.Core.Services;

public class CategorizationService(AppDbContext db, ClassificationSettingsService classificationSettings)
{
    /// <summary>
    /// Assigns a category to every uncategorized transaction: the learned merchant
    /// mapping if one matches.
    /// </summary>
    public async Task CategorizeAsync(IReadOnlyCollection<Transaction> transactions)
    {
        if (transactions.Count == 0) return;

        var settings = await classificationSettings.GetAsync();
        var matchMap = await db.MerchantCategoryMaps.ToDictionaryAsync(m => m.MatchKey, m => m.CategoryId);
        var fallbackId = await db.Categories
            .Where(c => c.IsFallback)
            .Select(c => (int?)c.Id)
            .FirstOrDefaultAsync();
        var savingsId = await db.Categories
            .Where(c => c.IsSavings)
            .Select(c => (int?)c.Id)
            .FirstOrDefaultAsync();

        foreach (var t in transactions.Where(t => !t.CategoryId.HasValue))
            t.CategoryId = MatchKey(t) is { } key && matchMap.TryGetValue(key, out var categoryId)
                ? categoryId
                : savingsId.HasValue && settings.IsSavings(t)
                    ? savingsId
                    : fallbackId;
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
        foreach (var t in transactions)
            t.CategoryId = null;

        await CategorizeAsync(transactions);
        await db.SaveChangesAsync();
        return transactions.Count;
    }

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
