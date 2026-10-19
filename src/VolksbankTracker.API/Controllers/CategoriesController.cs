using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VolksbankTracker.API.Models;
using VolksbankTracker.Core.Data;
using VolksbankTracker.Core.Services;

namespace VolksbankTracker.API.Controllers;

[ApiController]
[Route("api/categories")]
public class CategoriesController(
    AppDbContext db,
    CategorizationService categorization) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get() =>
        Ok(await db.Categories
            .OrderBy(c => c.Id)
            .Select(c => c.ToDto())
            .ToListAsync());

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateCategoryRequest request)
    {
        var name = request.Name.Trim();

        
        var existingNames = await db.Categories.Select(c => c.Name).ToListAsync();
        if (existingNames.Contains(name, StringComparer.OrdinalIgnoreCase))
            return Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Category already exists",
                detail: $"Eine Kategorie namens '{name}' existiert bereits.");

        var category = new Category { Name = name, Color = request.Color, Icon = request.Icon, IsIncome = request.IsIncome };
        db.Categories.Add(category);
        await db.SaveChangesAsync();

        return CreatedAtAction(nameof(Get), null, category.ToDto());
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateCategoryRequest updated)
    {
        var cat = await db.Categories.FindAsync(id);
        if (cat is null) return NotFound();
        // Every uncategorized incoming payment lands in the fallback, so flagging it would count all of them as income.
        if (cat.IsFallback && updated.IsIncome == true)
            return Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Fallback category cannot be income",
                detail: $"Die Kategorie '{cat.Name}' ist die Auffangkategorie und kann nicht als Einkommen markiert werden.");
        cat.Name = updated.Name;
        cat.Color = updated.Color;
        if (updated.Icon is not null)
            cat.Icon = updated.Icon;
        if (updated.IsIncome is not null)
            cat.IsIncome = updated.IsIncome.Value;
        await db.SaveChangesAsync();
        return Ok(cat.ToDto());
    }

    [HttpPost("recategorize")]
    public async Task<IActionResult> Recategorize()
    {
        var recategorized = await categorization.RecategorizeAllAsync();
        return Ok(new RecategorizeResult(recategorized));
    }
}
