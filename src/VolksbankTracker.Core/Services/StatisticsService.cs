using Microsoft.EntityFrameworkCore;
using VolksbankTracker.Core.Data;

namespace VolksbankTracker.Core.Services;

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
        var (classified, summaries) = await ClassifyAndSummarizeAsync(window);

        // The current month is still running it would drag every average down,
        // so averages use completed months only.
        var completedMonths = summaries
            .Where(m => new StatsMonth(m.Year, m.Month) != window.CurrentMonth)
            .ToList();
        var currentMonth = summaries
            .FirstOrDefault(m => new StatsMonth(m.Year, m.Month) == window.CurrentMonth);

        decimal Average(Func<MonthSummary, decimal> selector) =>
            completedMonths.Count > 0 ? completedMonths.Average(selector) : 0;

        var lastSyncedAt = await db.SyncLogs
            .Where(l => l.Status == SyncStatus.Success)
            .OrderByDescending(l => l.StartedAt)
            .Select(l => (DateTime?)(l.CompletedAt ?? l.StartedAt))
            .FirstOrDefaultAsync();

        return new DashboardStats(
            WindowFrom: window.From,
            WindowTo:   window.To,
            AverageMonthlyIncome:   Math.Round(Average(m => m.Income),      2),
            AverageMonthlyExpenses: Math.Round(Average(m => m.Expenses),    2),
            AverageMonthlySavings:  Math.Round(Average(m => m.Savings),     2),
            AverageSavingsRate:     Math.Round(Average(m => m.SavingsRate), 1),
            CurrentMonthIncome:   Math.Round(currentMonth?.Income   ?? 0, 2),
            CurrentMonthExpenses: Math.Round(currentMonth?.Expenses ?? 0, 2),
            CurrentMonthSavings:  Math.Round(currentMonth?.Savings  ?? 0, 2),
            TransactionsInWindow: classified.Count,
            Months:               summaries.Select(m => new DashboardMonthSummary(
                                       m.Year, m.Month, m.Income, m.Expenses, m.Savings, m.SavingsRate)).ToList(),
            TopExpenseCategories: TopExpenseCategories(classified, take: 6),
            LastSyncedAt:         lastSyncedAt
        );
    }

    public async Task<List<MonthSummary>> GetMonthlyBreakdownAsync(int months = 24)
    {
        var window = StatsWindow.LastMonthsIncludingCurrent(months, DateTime.UtcNow);
        var (_, summaries) = await ClassifyAndSummarizeAsync(window);
        return summaries;
    }

    private async Task<(List<ClassifiedTransaction> Classified, List<MonthSummary> Summaries)>
        ClassifyAndSummarizeAsync(StatsWindow window)
    {
        var settings = await classificationSettings.GetAsync();

        var transactions = await db.Transactions
            .Include(t => t.Category)
            .Where(t => t.BookingDate >= window.From)
            .ToListAsync();

        var classified = Classify(transactions, settings);
        return (classified, SummarizePerMonth(AttributeToMonth(classified, settings.SalaryConvention, window)));
    }

    // classification

    private static List<ClassifiedTransaction> Classify(
        List<Data.Transaction> transactions, ClassificationSettings settings) =>
        transactions
            .Select(t => new ClassifiedTransaction(t, ClassifyTransaction(t, settings)))
            .ToList();

    private static TransactionKind ClassifyTransaction(Data.Transaction t, ClassificationSettings s)
    {
      
        if (t.Category?.IsSavings == true)
            return t.Amount < 0 ? TransactionKind.Savings : TransactionKind.Excluded;

        // Outgoing internal transfer (same-bank own-account move)
        if (t.Amount < 0 && t.Purpose.Contains("interne Umbuchung", StringComparison.OrdinalIgnoreCase))
            return TransactionKind.Excluded;

        // Known income: salary
        if (t.Amount > 0 && ContainsAny(t.DebtorName, s.SalaryDebtorNames))
            return TransactionKind.Salary;

        // Known income: cash deposits
        if (t.Amount > 0 && ContainsAny(t.Purpose, s.CashDepositKeywords))
            return TransactionKind.Income;

        // All other positive transactions excluded
        if (t.Amount > 0)
            return TransactionKind.Excluded;

        return TransactionKind.Expense;
    }

    private static bool ContainsAny(string text, List<string> terms) =>
        terms.Any(term => text.Contains(term, StringComparison.OrdinalIgnoreCase));

    // split to the months

    private static IEnumerable<IGrouping<StatsMonth, ClassifiedTransaction>> AttributeToMonth(
        List<ClassifiedTransaction> classified, SalaryMonthConvention convention, StatsWindow window) =>
        classified
            .Where(x => x.Kind != TransactionKind.Excluded)
            .GroupBy(x => EffectiveMonth(x, convention))
            .Where(g => window.Contains(g.Key));

    /// <summary>
    /// Under <see cref="SalaryMonthConvention.PreviousMonth"/>, a salary booked
    /// anywhere in July counts toward June; the exact payday (1st, 15th, etc.)
    /// is deliberately irrelevant.
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
                decimal Total(params TransactionKind[] kinds) =>
                    Math.Abs(g.Where(x => kinds.Contains(x.Kind)).Sum(x => x.Transaction.Amount));

                var income = Total(TransactionKind.Salary, TransactionKind.Income);
                var expenses = Total(TransactionKind.Expense);
                var savings = Total(TransactionKind.Savings);

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
