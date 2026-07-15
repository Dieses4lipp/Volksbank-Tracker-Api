namespace VolksbankTracker.Core.Services;

public record StatsWindow(DateTime From, DateTime To, int MonthsBack)
{
    public static StatsWindow CompletedMonthsPlusCurrent(int monthsBack, DateTime today) =>
        new(new DateTime(today.Year, today.Month, 1).AddMonths(-monthsBack), today, monthsBack);

    public static StatsWindow LastMonthsIncludingCurrent(int months, DateTime today) =>
        new(new DateTime(today.Year, today.Month, 1).AddMonths(-months + 1), today, months);

    public StatsMonth CurrentMonth => StatsMonth.Of(To);

    public StatsMonth FirstMonth => StatsMonth.Of(From);

    public bool Contains(StatsMonth month)
    {
        var value = (month.Year, month.Month);
        return value.CompareTo((From.Year, From.Month)) >= 0
            && value.CompareTo((To.Year, To.Month)) <= 0;
    }
}
