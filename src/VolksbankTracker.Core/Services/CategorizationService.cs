using Microsoft.EntityFrameworkCore;
using VolksbankTracker.Core.Data;

namespace VolksbankTracker.Core.Services;

public class CategorizationService(AppDbContext db)
{
    public async Task CategorizeAsync(Transaction t, List<Category>? categories = null)
    {
        if (t.CategoryId.HasValue) return;

        categories ??= await db.Categories.ToListAsync();
        var searchText = $"{t.Purpose} {t.CreditorName} {t.DebtorName}".ToLowerInvariant();

        if (t.Amount > 0)
        {
            var income = categories.FirstOrDefault(c => c.IsIncome && MatchesKeywords(c, searchText));
            if (income is not null)
            {
                t.CategoryId = income.Id;
                return;
            }
        }

        var match = categories.FirstOrDefault(c => MatchesKeywords(c, searchText));
        t.CategoryId = match?.Id ?? categories.FirstOrDefault(c => c.IsFallback)?.Id;
    }

    public async Task<int> RecategorizeAllAsync()
    {
        var transactions = await db.Transactions.ToListAsync();
        var categories = await db.Categories.ToListAsync();
        foreach (var t in transactions)
        {
            t.CategoryId = null;
            await CategorizeAsync(t, categories);
        }
        await db.SaveChangesAsync();
        return transactions.Count;
    }

    private static bool MatchesKeywords(Category c, string searchText) =>
        !string.IsNullOrWhiteSpace(c.Keywords) &&
        c.Keywords
            .Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Any(k => searchText.Contains(k, StringComparison.OrdinalIgnoreCase));
}
