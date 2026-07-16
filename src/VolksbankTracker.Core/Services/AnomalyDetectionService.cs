using Microsoft.EntityFrameworkCore;
using VolksbankTracker.Core.Data;

namespace VolksbankTracker.Core.Services;

public class AnomalyDetectionService(AppDbContext db)
{
    private const int MinSampleSize = 5;

    public async Task<List<AnomalyTransaction>> DetectAsync(int months = 12, double stdDevThreshold = 2.5)
    {
        var since = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1).AddMonths(-months + 1);

        var expenses = await db.Transactions
            .Include(t => t.Category)
            .Where(t => t.Amount < 0 && t.BookingDate >= since && !t.IsRecurring)
            .ToListAsync();

        var anomalies = new List<AnomalyTransaction>();

        foreach (var group in expenses.GroupBy(t => t.CategoryId))
        {
            var items = group.ToList();
            if (items.Count < MinSampleSize) continue;

            var magnitudes = items.Select(t => Math.Abs(t.Amount)).ToList();
            var mean = magnitudes.Average();
            var variance = magnitudes.Sum(m => (m - mean) * (m - mean)) / magnitudes.Count;
            var stdDev = (decimal)Math.Sqrt((double)variance);

            if (stdDev == 0) continue;

            var threshold = mean + (decimal)stdDevThreshold * stdDev;

            anomalies.AddRange(items
                .Where(t => Math.Abs(t.Amount) > threshold)
                .Select(t => new AnomalyTransaction(
                    t.Id,
                    t.BookingDate,
                    t.Amount,
                    t.Purpose,
                    t.CreditorName,
                    t.CategoryId,
                    t.Category?.Name ?? "Sonstiges",
                    Math.Round(mean, 2),
                    Math.Round(stdDev, 2),
                    Math.Round((Math.Abs(t.Amount) - mean) / stdDev, 2)
                )));
        }

        return anomalies.OrderByDescending(a => a.DeviationFactor).ToList();
    }
}
