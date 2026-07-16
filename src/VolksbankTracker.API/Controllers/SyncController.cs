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
    FinTsSyncService sync,
    AppDbContext db) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Sync(SyncRequest? req)
    {
        if (FinTsNotConfigured() is { } error) return error;

        var result = await sync.SyncAsync(options.Value, req?.FromDate);
        return result.Succeeded ? Ok(result) : BankError(result.Error);
    }

    [HttpGet("balance")]
    public async Task<IActionResult> Balance()
    {
        if (FinTsNotConfigured() is { } error) return error;

        var result = await sync.GetBalanceAsync(options.Value);
        return result.Succeeded ? Ok(result) : BankError(result.Error);
    }

    [HttpGet("logs")]
    public async Task<IActionResult> Logs() =>
        Ok(await db.SyncLogs
            .OrderByDescending(l => l.StartedAt)
            .Take(20)
            .Select(l => l.ToDto())
            .ToListAsync());

    private IActionResult? FinTsNotConfigured() =>
        options.Value.IsComplete
            ? null
            : Problem(
                statusCode: StatusCodes.Status503ServiceUnavailable,
                title: "FinTS not configured",
                detail: "FinTs configuration is incomplete (BankUrl, Blz, Iban required). Set them via user-secrets.");

    private IActionResult BankError(string? detail) =>
        Problem(
            statusCode: StatusCodes.Status502BadGateway,
            title: "Bank communication failed",
            detail: detail);
}
