using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VolksbankTracker.API.Models;
using VolksbankTracker.Core.Data;
using VolksbankTracker.Core.Services;

namespace VolksbankTracker.API.Controllers;

[ApiController]
[Route("api/transactions")]
public class TransactionsController(AppDbContext db, CategorizationService categorization) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(int page = 1, int pageSize = 50,
        int? categoryId = null, string? search = null, string? type = null,
        string sortBy = "date", string sortDir = "desc")
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

        var descending = !string.Equals(sortDir, "asc", StringComparison.OrdinalIgnoreCase);
        query = sortBy.ToLowerInvariant() switch
        {
            "amount" => descending ? query.OrderByDescending(t => (double)t.Amount) : query.OrderBy(t => (double)t.Amount),
            "category" => descending
                ? query.OrderByDescending(t => t.Category!.Name).ThenByDescending(t => t.BookingDate)
                : query.OrderBy(t => t.Category!.Name).ThenBy(t => t.BookingDate),
            _ => descending ? query.OrderByDescending(t => t.BookingDate) : query.OrderBy(t => t.BookingDate),
        };

        var total = await query.CountAsync();
        var items = await query
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
        if (body.CategoryId.HasValue)
            await categorization.LearnAsync(t, body.CategoryId.Value);
        await db.SaveChangesAsync();
        await db.Entry(t).Reference(x => x.Category).LoadAsync();
        return Ok(t.ToDto());
    }

    private static string EscapeLike(string input) =>
        input.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_");
}
