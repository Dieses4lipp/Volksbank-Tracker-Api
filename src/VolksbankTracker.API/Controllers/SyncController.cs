using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using VolksbankTracker.API.Models;
using VolksbankTracker.Core.Data;
using VolksbankTracker.Core.Services;

namespace VolksbankTracker.API.Controllers;

[ApiController]
[Route("api/sync")]
public class SyncController(
    FinTsCredentialsService credentials,
    FinTsSyncService sync,
    AppDbContext db) : ControllerBase
{
    [HttpPost]
    [EnableRateLimiting(RateLimitPolicies.Sync)]
    public Task<IActionResult> Sync(SyncRequest? req) =>
        CallBankAsync(async config => await sync.SyncAsync(config, req?.FromDate));

    [HttpGet("balance")]
    [EnableRateLimiting(RateLimitPolicies.Sync)]
    public Task<IActionResult> Balance() =>
        CallBankAsync(async config => await sync.GetBalanceAsync(config));

    [HttpGet("logs")]
    public async Task<IActionResult> Logs() =>
        Ok(await db.SyncLogs
            .OrderByDescending(l => l.StartedAt)
            .Take(20)
            .Select(l => l.ToDto())
            .ToListAsync());

    /// <summary>Resolves the credentials, runs the bank call and maps its result to 200 / 502 (503 without credentials).</summary>
    private async Task<IActionResult> CallBankAsync(Func<FinTsConfig, Task<BankOperationResult>> call)
    {
        var (config, source) = await credentials.GetAsync();
        if (config is not { IsComplete: true }) return FinTsNotConfigured(source);

        var result = await call(config);
        return result.Succeeded ? Ok(result) : this.BankError(result.Error);
    }

    private ObjectResult FinTsNotConfigured(FinTsCredentialsSource source)
    {
        var (title, detail) = source == FinTsCredentialsSource.Unreadable
            ? ("FinTS credentials unreadable",
               "Stored FinTS credentials cannot be decrypted (Data Protection key ring changed?). Re-submit them via PUT /api/settings/fints.")
            : ("FinTS not configured",
               "No FinTS credentials available (BankUrl, BlZ, Iban, UserId, Pin required). Submit them via PUT /api/settings/fints or set the FinTs configuration section.");

        return Problem(statusCode: StatusCodes.Status503ServiceUnavailable, title: title, detail: detail);
    }
}
