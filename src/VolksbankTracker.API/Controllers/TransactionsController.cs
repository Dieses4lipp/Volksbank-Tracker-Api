using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VolksbankTracker.Core.Data;

namespace VolksbankTracker.API.Controllers;

[ApiController]
[Route("api/transactions")]
public class TransactionsController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(int page = 1, int pageSize = 50,
        int? categoryId = null, string? search = null, string? type = null)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 200);

        var query = db.Transactions.Include(t => t.Category).AsQueryable();

        if (categoryId.HasValue)
            query = query.Where(t => t.CategoryId == categoryId);

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(t =>
                t.Purpose.Contains(search) ||
                t.CreditorName.Contains(search) ||
                t.DebtorName.Contains(search));

        if (type == "income")
            query = query.Where(t => t.Amount > 0);
        else if (type == "expense")
            query = query.Where(t => t.Amount < 0);

        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(t => t.BookingDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return Ok(new { total, page, pageSize, items });
    }

    [HttpPatch("{id:int}/category")]
    public async Task<IActionResult> UpdateCategory(int id, [FromBody] UpdateCategoryRequest body)
    {
        var t = await db.Transactions.FindAsync(id);
        if (t is null) return NotFound();
        t.CategoryId = body.CategoryId;
        await db.SaveChangesAsync();
        return Ok(t);
    }
}

public record UpdateCategoryRequest(int CategoryId);
