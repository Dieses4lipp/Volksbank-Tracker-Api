using VolksbankTracker.Core.Data;

namespace VolksbankTracker.API.Models;

public record TransactionDto(
    int Id,
    DateTime BookingDate,
    DateTime ValueDate,
    decimal Amount,
    string Currency,
    string Purpose,
    string CreditorName,
    string CreditorIban,
    string DebtorName,
    string DebtorIban,
    int? CategoryId,
    CategoryDto? Category,
    bool IsRecurring);

public static class TransactionDtoMapping
{
    public static TransactionDto ToDto(this Transaction t) => new(
        t.Id, t.BookingDate, t.ValueDate, t.Amount, t.Currency, t.Purpose,
        t.CreditorName, t.CreditorIban, t.DebtorName, t.DebtorIban,
        t.CategoryId, t.Category?.ToDto(), t.IsRecurring);
}
