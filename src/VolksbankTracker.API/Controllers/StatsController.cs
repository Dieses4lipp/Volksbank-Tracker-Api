using Microsoft.AspNetCore.Mvc;
using VolksbankTracker.Core.Services;

namespace VolksbankTracker.API.Controllers;

[ApiController]
[Route("api/stats")]
public class StatsController(
    StatisticsService stats,
    AnomalyDetectionService anomalies) : ControllerBase
{
    [HttpGet("dashboard")]
    public async Task<IActionResult> Dashboard() =>
        Ok(await stats.GetDashboardStatsAsync());

    [HttpGet("monthly")]
    public async Task<IActionResult> Monthly(int months = 24) =>
        Ok(await stats.GetMonthlyBreakdownAsync(Math.Clamp(months, 1, 120)));

    [HttpGet("anomalies")]
    public async Task<IActionResult> Anomalies(int months = 12, double threshold = 2.5) =>
        Ok(await anomalies.DetectAsync(Math.Clamp(months, 1, 120), Math.Clamp(threshold, 0.5, 10)));
}
