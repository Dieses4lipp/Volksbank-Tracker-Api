namespace VolksbankTracker.Core.Services;

/// <summary>
/// The calendar month an amount is attributed to. May differ from the booking
/// month see <see cref="SalaryMonthConvention"/>.
/// </summary>
public readonly record struct StatsMonth(int Year, int Month)
{
    public static StatsMonth Of(DateTime date) => new(date.Year, date.Month);
}
