using VolksbankTracker.Core.Data;

namespace VolksbankTracker.API.Models;

public record CategoryDto(int Id, string Name, string Icon, string Color, bool IsFallback, bool IsSavings);

public static class CategoryDtoMapping
{
    public static CategoryDto ToDto(this Category c) =>
        new(c.Id, c.Name, c.Icon, c.Color, c.IsFallback, c.IsSavings);
}
