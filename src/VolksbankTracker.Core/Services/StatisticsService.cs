using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using VolksbankTracker.Core.Data;

namespace VolksbankTracker.Core.Services;

public record MonthSummary(
    int Year,
    int Month,
    decimal Income,
    decimal Expenses,
    decimal Savings,
    decimal Balance,
    decimal SavingsRate
);

public record CategoryBreakdown(
    string CategoryName,
    string Icon,
    string Color,
    decimal Total,
    int Count
);

public record DashboardStats(
    decimal AverageMonthlyIncome,
    decimal AverageMonthlyExpenses,
    decimal AverageMonthlySavings,
    decimal AverageSavingsRate,
    decimal CurrentMonthIncome,
    decimal CurrentMonthExpenses,
    decimal CurrentMonthSavings,
    int TotalTransactions,
    List<MonthSummary> Last12Months,
    List<CategoryBreakdown> TopExpenseCategories,
    DateTime? LastSyncedAt
);

public class StatisticsService(AppDbContext db, IConfiguration config)
{
    private List<string> SavingsCreditorNames =>
        config.GetSection("FinTsClassification:SavingsCreditorNames").Get<List<string>>() ?? [];
    private List<string> SavingsIbans =>
        config.GetSection("FinTsClassification:SavingsIbans").Get<List<string>>() ?? [];
    private List<string> SalaryDebtorNames =>
        config.GetSection("FinTsClassification:SalaryDebtorNames").Get<List<string>>() ?? [];
    private List<string> CashDepositKeywords =>
        config.GetSection("FinTsClassification:CashDepositKeywords").Get<List<string>>() ?? [];

    private TransactionKind ClassifyTransaction(Data.Transaction t)
    {
        // Savings: outgoing to savings IBAN (own savings account)
        if (t.Amount < 0 && !string.IsNullOrEmpty(t.CreditorIban) && SavingsIbans.Contains(t.CreditorIban))
            return TransactionKind.Savings;

        // Savings: outgoing to named savings institution (Bausparkasse etc.)
        if (t.Amount < 0 && SavingsCreditorNames.Any(n => t.CreditorName?.Contains(n, StringComparison.OrdinalIgnoreCase) == true))
            return TransactionKind.Savings;

        // Outgoing internal transfer (same-bank own-account move)
        if (t.Amount < 0 && t.Purpose?.Contains("interne Umbuchung", StringComparison.OrdinalIgnoreCase) == true)
            return TransactionKind.Excluded;

        // Known income: salary
        if (t.Amount > 0 && SalaryDebtorNames.Any(n => t.DebtorName?.Contains(n, StringComparison.OrdinalIgnoreCase) == true))
            return TransactionKind.Salary;

        // Known income: cash deposits
        if (t.Amount > 0 && CashDepositKeywords.Any(k => t.Purpose?.Contains(k, StringComparison.OrdinalIgnoreCase) == true))
            return TransactionKind.Income;

        // All other positive transactions (family, own account, refunds) excluded
        if (t.Amount > 0)
            return TransactionKind.Excluded;

        return TransactionKind.Expense;
    }

    private static (int Year, int Month) GetEffectivePeriod(Data.Transaction t, TransactionKind kind)
    {
        var date = kind == TransactionKind.Salary ? t.BookingDate.AddMonths(-1) : t.BookingDate;
        return (date.Year, date.Month);
    }

    private List<MonthSummary> BuildMonthSummaries(List<Data.Transaction> transactions)
    {
        var classified = transactions
            .Select(t => (t, kind: ClassifyTransaction(t)))
            .Where(x => x.kind != TransactionKind.Excluded)
            .ToList();

        return classified
            .GroupBy(x => GetEffectivePeriod(x.t, x.kind))
            .Select(g =>
            {
                var income = g.Where(x => x.kind is TransactionKind.Salary or TransactionKind.Income)
                              .Sum(x => x.t.Amount);

                var expenses = Math.Abs(g.Where(x => x.kind == TransactionKind.Expense)
                                         .Sum(x => x.t.Amount));

                var savings = Math.Abs(g.Where(x => x.kind == TransactionKind.Savings)
                                        .Sum(x => x.t.Amount));

                var balance = income - expenses;

                return new MonthSummary(
                    g.Key.Year, g.Key.Month,
                    Income: income,
                    Expenses: expenses,
                    Savings: savings,
                    Balance: balance,
                    SavingsRate: income > 0 ? savings / income * 100 : 0
                );
            })
            .OrderBy(m => m.Year).ThenBy(m => m.Month)
            .ToList();
    }

    public async Task<DashboardStats> GetDashboardStatsAsync()
    {
        var now = DateTime.UtcNow;
        var twelveMonthsAgo = new DateTime(now.Year, now.Month, 1).AddMonths(-12);

        var transactions = await db.Transactions
            .Include(t => t.Category)
            .Where(t => t.BookingDate >= twelveMonthsAgo)
            .ToListAsync();

        var grouped = BuildMonthSummaries(transactions);

        var completedMonths = grouped.Where(m =>
            !(m.Year == now.Year && m.Month == now.Month)).ToList();

        var avgIncome   = completedMonths.Any() ? completedMonths.Average(m => m.Income)   : 0;
        var avgExpenses = completedMonths.Any() ? completedMonths.Average(m => m.Expenses) : 0;
        var avgSavings  = completedMonths.Any() ? completedMonths.Average(m => m.Savings)  : 0;
        var avgRate     = completedMonths.Any() ? completedMonths.Average(m => m.SavingsRate) : 0;

        var currentMonth = grouped.FirstOrDefault(m => m.Year == now.Year && m.Month == now.Month);

        var classifiedLookup = transactions.ToDictionary(t => t.Id, t => ClassifyTransaction(t));

        var categoryBreakdown = transactions
            .Where(t => t.Amount < 0 && t.Category != null
                && classifiedLookup[t.Id] == TransactionKind.Expense)
            .GroupBy(t => t.Category!)
            .Select(g => new CategoryBreakdown(
                g.Key.Name,
                g.Key.Icon,
                g.Key.Color,
                Math.Abs(g.Sum(t => t.Amount)),
                g.Count()
            ))
            .OrderByDescending(c => c.Total)
            .Take(6)
            .ToList();

        var lastSyncedAt = await db.SyncLogs
            .Where(l => l.Status == "success")
            .OrderByDescending(l => l.StartedAt)
            .Select(l => (DateTime?)(l.CompletedAt ?? l.StartedAt))
            .FirstOrDefaultAsync();

        return new DashboardStats(
            AverageMonthlyIncome:   Math.Round(avgIncome,   2),
            AverageMonthlyExpenses: Math.Round(avgExpenses, 2),
            AverageMonthlySavings:  Math.Round(avgSavings,  2),
            AverageSavingsRate:     Math.Round(avgRate,     1),
            CurrentMonthIncome:   Math.Round(currentMonth?.Income   ?? 0, 2),
            CurrentMonthExpenses: Math.Round(currentMonth?.Expenses ?? 0, 2),
            CurrentMonthSavings:  Math.Round(currentMonth?.Savings  ?? 0, 2),
            TotalTransactions: transactions.Count,
            Last12Months:           grouped,
            TopExpenseCategories:   categoryBreakdown,
            LastSyncedAt:           lastSyncedAt
        );
    }

    private enum TransactionKind
    {
        Income, Expense, Savings, Salary, Excluded
    }

    public async Task<List<MonthSummary>> GetMonthlyBreakdownAsync(int months = 24)
    {
        var since = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1).AddMonths(-months + 1);
        var transactions = await db.Transactions
            .Where(t => t.BookingDate >= since)
            .ToListAsync();

        return BuildMonthSummaries(transactions);
    }
}
