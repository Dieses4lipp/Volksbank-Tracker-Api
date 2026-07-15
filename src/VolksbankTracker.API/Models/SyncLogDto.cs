using VolksbankTracker.Core.Data;

namespace VolksbankTracker.API.Models;

public record SyncLogDto(
    int Id,
    DateTime StartedAt,
    DateTime? CompletedAt,
    int TransactionsFetched,
    int TransactionsNew,
    string Status,
    string? Error);

public static class SyncLogDtoMapping
{
    public static SyncLogDto ToDto(this SyncLog l) =>
        new(l.Id, l.StartedAt, l.CompletedAt, l.TransactionsFetched, l.TransactionsNew, l.Status, l.Error);
}
