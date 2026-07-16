using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VolksbankTracker.API.Models;
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
        { 
            var pattern = $"%{EscapeLike(search)}%";
            query = query.Where(t =>
                EF.Functions.Like(t.Purpose, pattern, "\\") ||
                EF.Functions.Like(t.CreditorName, pattern, "\\") ||
                EF.Functions.Like(t.DebtorName, pattern, "\\"));
        }

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

        return Ok(new PagedResult<TransactionDto>(
            total, page, pageSize, items.Select(t => t.ToDto()).ToList()));
    }

    [HttpPatch("{id:int}/category")]
    public async Task<IActionResult> UpdateCategory(int id, [FromBody] AssignCategoryRequest body)
    {
        var t = await db.Transactions.FindAsync(id);
        if (t is null) return NotFound();

        if (body.CategoryId.HasValue &&
            !await db.Categories.AnyAsync(c => c.Id == body.CategoryId))
            return Problem(
                statusCode: StatusCodes.Status400BadRequest,
                detail: $"Category {body.CategoryId} does not exist.");

        t.CategoryId = body.CategoryId;
        await db.SaveChangesAsync();
        await db.Entry(t).Reference(x => x.Category).LoadAsync();
        return Ok(t.ToDto());
    }

    private static string EscapeLike(string input) =>
        input.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_");
}
