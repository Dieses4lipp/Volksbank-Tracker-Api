using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using VolksbankTracker.API.Models;
using VolksbankTracker.Core.Data;
using VolksbankTracker.Core.Services;

namespace VolksbankTracker.API.Controllers;

[ApiController]
[Route("api/sync")]
public class SyncController(
    IOptions<FinTsConfig> options,
    FinTsSyncService svc,
    AppDbContext db) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Sync(SyncRequest? req)
    {
        var result = await svc.SyncAsync(RequireFinTsConfig(), req?.FromDate);
        return Ok(result);
    }

    [HttpGet("balance")]
    public async Task<IActionResult> Balance()
    {
        var result = await svc.GetBalanceAsync(RequireFinTsConfig());
        return Ok(result);
    }

    [HttpGet("logs")]
    public async Task<IActionResult> Logs() =>
        Ok(await db.SyncLogs
            .OrderByDescending(l => l.StartedAt)
            .Take(20)
            .Select(l => l.ToDto())
            .ToListAsync());

    private FinTsConfig RequireFinTsConfig()
    {
        var cfg = options.Value;
        if (string.IsNullOrWhiteSpace(cfg.BankUrl) ||
            string.IsNullOrWhiteSpace(cfg.BlZ) ||
            string.IsNullOrWhiteSpace(cfg.Iban))
            throw new InvalidOperationException(
                "FinTs configuration is incomplete (BankUrl, Blz, Iban required). Set them via user-secrets.");
        return cfg;
    }
}
