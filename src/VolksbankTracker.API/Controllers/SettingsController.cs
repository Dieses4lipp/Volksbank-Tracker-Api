using Microsoft.AspNetCore.Mvc;
using VolksbankTracker.Core.Services;

namespace VolksbankTracker.API.Controllers;

[ApiController]
[Route("api/settings")]
public class SettingsController(ClassificationSettingsService settings) : ControllerBase
{
    [HttpGet("classification")]
    public async Task<IActionResult> GetClassification() =>
        Ok(await settings.GetAsync());

    [HttpPut("classification")]
    public async Task<IActionResult> PutClassification(ClassificationSettings body) =>
        Ok(await settings.SaveAsync(body));
}
