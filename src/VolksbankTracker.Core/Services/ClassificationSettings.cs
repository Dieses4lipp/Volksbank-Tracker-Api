namespace VolksbankTracker.Core.Services;

public record ClassificationSettings(
    List<string> SavingsIbans,
    List<string> SavingsCreditorNames,
    List<string> SalaryDebtorNames,
    List<string> CashDepositKeywords,
    SalaryMonthConvention SalaryConvention = SalaryMonthConvention.PreviousMonth)
{
    public static ClassificationSettings Empty => new([], [], [], []);

    public ClassificationSettings Normalized() => new(
        Clean(SavingsIbans),
        Clean(SavingsCreditorNames),
        Clean(SalaryDebtorNames),
        Clean(CashDepositKeywords),
        SalaryConvention);

    private static List<string> Clean(List<string>? items) =>
        (items ?? [])
            .Select(i => i.Trim())
            .Where(i => i.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
}
