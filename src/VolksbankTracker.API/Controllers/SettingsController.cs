using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using VolksbankTracker.API.Models;
using VolksbankTracker.Core.Services;

namespace VolksbankTracker.API.Controllers;

[ApiController]
[Route("api/settings")]
public class SettingsController(
    ClassificationSettingsService settings,
    CategorizationService categorization,
    FinTsCredentialsService credentials,
    FinTsSyncService sync) : ControllerBase
{
    [HttpGet("classification")]
    public async Task<IActionResult> GetClassification() =>
        Ok(await settings.GetAsync());

    /// <summary>
    /// Saves the settings and recategorizes all transactions so the savings category follows them.
    /// </summary>
    [HttpPut("classification")]
    public async Task<IActionResult> PutClassification(ClassificationSettings body)
    {
        var saved = await settings.SaveAsync(body);
        await categorization.RecategorizeAllAsync();
        return Ok(saved);
    }

    [HttpGet("fints")]
    public async Task<IActionResult> GetFinTs()
    {
        var (config, source) = await credentials.GetAsync();
        return Ok(FinTsCredentialsStatusDto.From(config, source));
    }

    /// <summary>Verifies the credentials against the bank and stores them encrypted only if the bank accepts them.</summary>
    [HttpPut("fints")]
    [EnableRateLimiting(RateLimitPolicies.Credentials)]
    public async Task<IActionResult> PutFinTs(FinTsCredentialsRequest body)
    {
        var config = body.ToConfig();

        var check = await sync.VerifyCredentialsAsync(config);
        if (!check.Succeeded)
            return check.Rejected
                ? Problem(
                    statusCode: StatusCodes.Status422UnprocessableEntity,
                    title: "Bank rejected credentials",
                    detail: check.Error)
                : this.BankError(check.Error);

        await credentials.SaveAsync(config);
        return Ok(FinTsCredentialsStatusDto.From(config, FinTsCredentialsSource.Database));
    }

    [HttpDelete("fints")]
    public async Task<IActionResult> DeleteFinTs()
    {
        await credentials.DeleteAsync();
        return NoContent();
    }
}
