namespace VolksbankTracker.Core.Services;

/// <summary>Same as <see cref="MonthSummary"/> without Balance — used on the dashboard.</summary>
public record DashboardMonthSummary(
    int Year,
    int Month,
    decimal Income,
    decimal Expenses,
    decimal Savings,
    decimal SavingsRate
);
