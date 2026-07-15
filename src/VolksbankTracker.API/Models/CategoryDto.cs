using VolksbankTracker.Core.Data;

namespace VolksbankTracker.API.Models;

public record CategoryDto(int Id, string Name, string Icon, string Color, string Keywords, bool IsIncome, bool IsFallback);

public static class CategoryDtoMapping
{
    public static CategoryDto ToDto(this Category c) =>
        new(c.Id, c.Name, c.Icon, c.Color, c.Keywords, c.IsIncome, c.IsFallback);
}
