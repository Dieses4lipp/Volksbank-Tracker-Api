namespace VolksbankTracker.Core.Services;

public record StatsSummary(
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
    List<CategoryBreakdown> TopExpenseCategories,
    DateTime? LastSyncedAt
);
