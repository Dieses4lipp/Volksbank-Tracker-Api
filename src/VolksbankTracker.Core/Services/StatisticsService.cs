using Microsoft.EntityFrameworkCore;
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

/// <summary>Same as <see cref="MonthSummary"/> without Balance — used on the dashboard.</summary>
public record DashboardMonthSummary(
    int Year,
    int Month,
    decimal Income,
    decimal Expenses,
    decimal Savings,
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
    DateTime WindowFrom,
    DateTime WindowTo,
    decimal AverageMonthlyIncome,
    decimal AverageMonthlyExpenses,
    decimal AverageMonthlySavings,
    decimal AverageSavingsRate,
    decimal CurrentMonthIncome,
    decimal CurrentMonthExpenses,
    decimal CurrentMonthSavings,
    int TransactionsInWindow,
    List<DashboardMonthSummary> Months,
    List<CategoryBreakdown> TopExpenseCategories,
    DateTime? LastSyncedAt
);

public class StatisticsService(AppDbContext db, ClassificationSettingsService classificationSettings)
{
    private enum TransactionKind
    {
        Income, Expense, Savings, Salary, Excluded
    }

    private sealed record ClassifiedTransaction(Data.Transaction Transaction, TransactionKind Kind);

    public async Task<DashboardStats> GetDashboardStatsAsync()
    {
        // 12 completed months for the averages + the running current month.
        var window = StatsWindow.CompletedMonthsPlusCurrent(12, DateTime.UtcNow);
        var settings = await classificationSettings.GetAsync();

        var transactions = await db.Transactions
            .Include(t => t.Category)
            .Where(t => t.BookingDate >= window.From)
            .ToListAsync();

        var classified = Classify(transactions, settings);
        var summaries = SummarizePerMonth(AttributeToMonth(classified, settings.SalaryConvention, window));

        // The current month is still running it would drag every average down,
        // so averages use completed months only.
        var completedMonths = summaries
            .Where(m => new StatsMonth(m.Year, m.Month) != window.CurrentMonth)
            .ToList();
        var currentMonth = summaries
            .FirstOrDefault(m => new StatsMonth(m.Year, m.Month) == window.CurrentMonth);

        var avgIncome   = completedMonths.Count > 0 ? completedMonths.Average(m => m.Income)      : 0;
        var avgExpenses = completedMonths.Count > 0 ? completedMonths.Average(m => m.Expenses)    : 0;
        var avgSavings  = completedMonths.Count > 0 ? completedMonths.Average(m => m.Savings)     : 0;
        var avgRate     = completedMonths.Count > 0 ? completedMonths.Average(m => m.SavingsRate) : 0;

        var lastSyncedAt = await db.SyncLogs
            .Where(l => l.Status == "success")
            .OrderByDescending(l => l.StartedAt)
            .Select(l => (DateTime?)(l.CompletedAt ?? l.StartedAt))
            .FirstOrDefaultAsync();

        return new DashboardStats(
            WindowFrom: window.From,
            WindowTo:   window.To,
            AverageMonthlyIncome:   Math.Round(avgIncome,   2),
            AverageMonthlyExpenses: Math.Round(avgExpenses, 2),
            AverageMonthlySavings:  Math.Round(avgSavings,  2),
            AverageSavingsRate:     Math.Round(avgRate,     1),
            CurrentMonthIncome:   Math.Round(currentMonth?.Income   ?? 0, 2),
            CurrentMonthExpenses: Math.Round(currentMonth?.Expenses ?? 0, 2),
            CurrentMonthSavings:  Math.Round(currentMonth?.Savings  ?? 0, 2),
            TransactionsInWindow: transactions.Count,
            Months:               summaries.Select(m => new DashboardMonthSummary(
                                       m.Year, m.Month, m.Income, m.Expenses, m.Savings, m.SavingsRate)).ToList(),
            TopExpenseCategories: TopExpenseCategories(classified, take: 6),
            LastSyncedAt:         lastSyncedAt
        );
    }

    public async Task<List<MonthSummary>> GetMonthlyBreakdownAsync(int months = 24)
    {
        var window = StatsWindow.LastMonthsIncludingCurrent(months, DateTime.UtcNow);
        var settings = await classificationSettings.GetAsync();

        var transactions = await db.Transactions
            .Where(t => t.BookingDate >= window.From)
            .ToListAsync();

        var classified = Classify(transactions, settings);
        return SummarizePerMonth(AttributeToMonth(classified, settings.SalaryConvention, window));
    }

    // classification

    private static List<ClassifiedTransaction> Classify(
        List<Data.Transaction> transactions, ClassificationSettings settings) =>
        transactions
            .Select(t => new ClassifiedTransaction(t, ClassifyTransaction(t, settings)))
            .ToList();

    private static TransactionKind ClassifyTransaction(Data.Transaction t, ClassificationSettings s)
    {
        // Savings: outgoing to savings IBAN (own savings account)
        if (t.Amount < 0 && !string.IsNullOrEmpty(t.CreditorIban) && s.SavingsIbans.Contains(t.CreditorIban))
            return TransactionKind.Savings;

        // Savings: outgoing to named savings institution
        if (t.Amount < 0 && s.SavingsCreditorNames.Any(n => t.CreditorName?.Contains(n, StringComparison.OrdinalIgnoreCase) == true))
            return TransactionKind.Savings;

        // Outgoing internal transfer (same-bank own-account move)
        if (t.Amount < 0 && t.Purpose?.Contains("interne Umbuchung", StringComparison.OrdinalIgnoreCase) == true)
            return TransactionKind.Excluded;

        // Known income: salary
        if (t.Amount > 0 && s.SalaryDebtorNames.Any(n => t.DebtorName?.Contains(n, StringComparison.OrdinalIgnoreCase) == true))
            return TransactionKind.Salary;

        // Known income: cash deposits
        if (t.Amount > 0 && s.CashDepositKeywords.Any(k => t.Purpose?.Contains(k, StringComparison.OrdinalIgnoreCase) == true))
            return TransactionKind.Income;

        // All other positive transactions excluded
        if (t.Amount > 0)
            return TransactionKind.Excluded;

        return TransactionKind.Expense;
    }

    // split to the months 

    private static IEnumerable<IGrouping<StatsMonth, ClassifiedTransaction>> AttributeToMonth(
        List<ClassifiedTransaction> classified, SalaryMonthConvention convention, StatsWindow window) =>
        classified
            .Where(x => x.Kind != TransactionKind.Excluded)
            .GroupBy(x => EffectiveMonth(x, convention))
            .Where(g => window.Contains(g.Key));

    /// <summary>
    /// Under <see cref="SalaryMonthConvention.PreviousMonth"/> a salary booked
    /// anywhere in July counts toward June the exact payday (1st, 15th)
    /// is deliberately irrelevant
    /// </summary>
    private static StatsMonth EffectiveMonth(ClassifiedTransaction x, SalaryMonthConvention convention)
    {
        var date = x.Kind == TransactionKind.Salary && convention == SalaryMonthConvention.PreviousMonth
            ? x.Transaction.BookingDate.AddMonths(-1)
            : x.Transaction.BookingDate;
        return StatsMonth.Of(date);
    }

    // summarize and create the stats per month 

    private static List<MonthSummary> SummarizePerMonth(
        IEnumerable<IGrouping<StatsMonth, ClassifiedTransaction>> byMonth) =>
        byMonth
            .Select(g =>
            {
                var income = g.Where(x => x.Kind is TransactionKind.Salary or TransactionKind.Income)
                              .Sum(x => x.Transaction.Amount);

                var expenses = Math.Abs(g.Where(x => x.Kind == TransactionKind.Expense)
                                         .Sum(x => x.Transaction.Amount));

                var savings = Math.Abs(g.Where(x => x.Kind == TransactionKind.Savings)
                                        .Sum(x => x.Transaction.Amount));

                return new MonthSummary(
                    g.Key.Year, g.Key.Month,
                    Income: Math.Round(income, 2),
                    Expenses: Math.Round(expenses, 2),
                    Savings: Math.Round(savings, 2),
                    Balance: Math.Round(income - expenses, 2),
                    SavingsRate: income > 0 ? Math.Round(savings / income * 100, 1) : 0
                );
            })
            .OrderBy(m => m.Year).ThenBy(m => m.Month)
            .ToList();

    private static List<CategoryBreakdown> TopExpenseCategories(
        List<ClassifiedTransaction> classified, int take) =>
        classified
            .Where(x => x.Kind == TransactionKind.Expense && x.Transaction.Category != null)
            .GroupBy(x => x.Transaction.Category!)
            .Select(g => new CategoryBreakdown(
                g.Key.Name,
                g.Key.Icon,
                g.Key.Color,
                Math.Abs(g.Sum(x => x.Transaction.Amount)),
                g.Count()
            ))
            .OrderByDescending(c => c.Total)
            .Take(take)
            .ToList();
}
