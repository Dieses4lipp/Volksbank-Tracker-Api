using VolksbankTracker.Core.Data;

namespace VolksbankTracker.Core.Services;

public record ClassificationSettings(
    List<string> SavingsIbans,
    List<string> SavingsCreditorNames,
    List<string> SalaryDebtorNames,
    List<string> CashDepositKeywords,
    SalaryMonthConvention SalaryConvention = SalaryMonthConvention.PreviousMonth)
{
    public static ClassificationSettings Empty => new([], [], [], []);

    /// <summary>
    /// Outgoing transfer to an own savings IBAN or a named savings institution.
    /// </summary>
    public bool IsSavings(Transaction t) =>
        t.Amount < 0 &&
        ((t.CreditorIban.Length > 0 && SavingsIbans.Contains(t.CreditorIban, StringComparer.OrdinalIgnoreCase)) ||
         SavingsCreditorNames.Any(name => t.CreditorName.Contains(name, StringComparison.OrdinalIgnoreCase)));

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
