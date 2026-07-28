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
