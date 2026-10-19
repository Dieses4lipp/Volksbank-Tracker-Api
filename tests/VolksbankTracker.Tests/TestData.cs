using VolksbankTracker.Core.Data;

namespace VolksbankTracker.Tests;

/// <summary>Rows written identically to every provider, dated relative to today.</summary>
public static class TestData
{
    public static readonly DateTime Today = DateTime.UtcNow.Date;

    public static Transaction[] Transactions() =>
    [
        Expense("t1", Today, -54.30m, "Einkauf", "REWE Markt", categoryId: 3),
        Expense("t2", Today.AddDays(-40), -9.99m, "rewe to go", "Rewe To Go", categoryId: 3),
        Expense("t3", Today.AddDays(-40), -1200.00m, "Miete", "Vermieter", categoryId: 2),
        Expense("t4", Today.AddDays(-50), -12.50m, "Bio 100% Rabatt", "Hofladen", categoryId: null),
        Incoming("t5", Today, 1000.00m, "Rueckzahlung", "Max Mustermann", categoryId: 1),
    ];

    public static SyncLog SyncLog() => new()
    {
        StartedAt = DateTime.UtcNow.AddHours(-1),
        CompletedAt = DateTime.UtcNow.AddMinutes(-59),
        TransactionsFetched = 5,
        TransactionsNew = 5,
        Status = SyncStatus.Success
    };

    public static async Task SeedAsync(AppDbContext db)
    {
        db.Transactions.AddRange(Transactions());
        db.SyncLogs.Add(SyncLog());
        await db.SaveChangesAsync();
    }

    private static Transaction Expense(string hash, DateTime date, decimal amount, string purpose, string creditor, int? categoryId) => new()
    {
        Hash = hash,
        BookingDate = date,
        ValueDate = date,
        Amount = amount,
        Purpose = purpose,
        CreditorName = creditor,
        CategoryId = categoryId,
        // Postgres keeps microseconds, DateTime.UtcNow has 100 ns ticks.
        ImportedAt = Today.AddHours(8)
    };

    private static Transaction Incoming(string hash, DateTime date, decimal amount, string purpose, string debtor, int? categoryId) => new()
    {
        Hash = hash,
        BookingDate = date,
        ValueDate = date,
        Amount = amount,
        Purpose = purpose,
        DebtorName = debtor,
        CategoryId = categoryId,
        ImportedAt = Today.AddHours(8)
    };
}
