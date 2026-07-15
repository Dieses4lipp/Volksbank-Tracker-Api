using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VolksbankTracker.API.Models;
using VolksbankTracker.Core.Data;
using VolksbankTracker.Core.Services;

namespace VolksbankTracker.API.Controllers;

[ApiController]
[Route("api/categories")]
public class CategoriesController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get() =>
        Ok(await db.Categories
            .OrderBy(c => c.Id)
            .Select(c => c.ToDto())
            .ToListAsync());

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateCategoryRequest updated)
    {
        var cat = await db.Categories.FindAsync(id);
        if (cat is null) return NotFound();
        cat.Name = updated.Name;
        cat.Keywords = updated.Keywords;
        cat.Color = updated.Color;
        if (updated.Icon is not null)
            cat.Icon = updated.Icon;
        await db.SaveChangesAsync();
        return Ok(cat.ToDto());
    }

    [HttpPost("recategorize")]
    public async Task<IActionResult> Recategorize([FromServices] CategorizationService categorization)
    {
        var recategorized = await categorization.RecategorizeAllAsync();
        return Ok(new { recategorized });
    }
}
