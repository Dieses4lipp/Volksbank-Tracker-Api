namespace VolksbankTracker.Core.Services;

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
