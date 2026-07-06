using Microsoft.EntityFrameworkCore;
using VolksbankTracker.Core.Data;

namespace VolksbankTracker.Core.Services;

public class CategorizationService(AppDbContext db)
{
    private static readonly Dictionary<int, string[]> _builtinRules = new()
    {
        [1] = ["gehalt", "lohn", "entgelt", "gutschrift arbeitgeber"],
        [2] = ["miete", "nebenkosten"],
        [3] = ["rewe", "aldi", "lidl", "edeka", "netto", "kaufland", "nah und gut", "nah + gut", "marktkauf", "penny"],
        [4] = ["tank", "aral", "shell", "db bahn", "deutsche bahn", "vgn", "öpnv", "parken"],
        [5] = ["versicherung", "allianz", "huk", "aok", "tkk", "barmer", "gkv"],
        [6] = ["netflix", "spotify", "steam", "amazon prime", "disney", "kino", "restaurant", "lieferando"],
        [7] = ["apotheke", "arzt", "zahnarzt", "xtra", "fitnessstudio"],
    };

    public async Task CategorizeAsync(Transaction t, List<Category>? categories = null)
    {
        if (t.CategoryId.HasValue) return;

        var searchText = $"{t.Purpose} {t.CreditorName} {t.DebtorName}".ToLowerInvariant();

        if (t.Amount > 0)
        {
            if (_builtinRules[1].Any(k => searchText.Contains(k)))
            {
                t.CategoryId = 1;
                return;
            }
        }

        categories ??= await db.Categories.ToListAsync();
        foreach (var cat in categories.Where(c => !string.IsNullOrWhiteSpace(c.Keywords)))
        {
            var keywords = cat.Keywords.Split('|', StringSplitOptions.RemoveEmptyEntries);
            if (keywords.Any(k => searchText.Contains(k.Trim().ToLowerInvariant())))
            {
                t.CategoryId = cat.Id;
                return;
            }
        }

        foreach (var (catId, keywords) in _builtinRules)
        {
            if (keywords.Any(k => searchText.Contains(k)))
            {
                t.CategoryId = catId;
                return;
            }
        }

        t.CategoryId = 8;
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
}
