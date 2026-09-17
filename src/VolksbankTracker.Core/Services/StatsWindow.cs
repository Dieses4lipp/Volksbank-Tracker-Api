namespace VolksbankTracker.Core.Services;

public record StatsWindow(DateTime From, DateTime To)
{
    public static StatsWindow CompletedMonthsPlusCurrent(int monthsBack, DateTime today) =>
        new(MonthStart(today).AddMonths(-monthsBack), today);

    public static StatsWindow LastMonthsIncludingCurrent(int months, DateTime today) =>
        new(MonthStart(today).AddMonths(-months + 1), today);

    public StatsMonth CurrentMonth => StatsMonth.Of(To);

    public bool Contains(StatsMonth month)
    {
        var value = (month.Year, month.Month);
        return value.CompareTo((From.Year, From.Month)) >= 0
            && value.CompareTo((To.Year, To.Month)) <= 0;
    }

    private static DateTime MonthStart(DateTime date) => new(date.Year, date.Month, 1);
}
