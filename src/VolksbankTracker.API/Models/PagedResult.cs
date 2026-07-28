namespace VolksbankTracker.API.Models;

public record PagedResult<T>(int Total, int Page, int PageSize, IReadOnlyList<T> Items);
