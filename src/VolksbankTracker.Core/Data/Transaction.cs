namespace VolksbankTracker.Core.Data;

public class Transaction
{
    public int Id { get; set; }
    public string Hash { get; set; } = "";          // SHA256 of date+amount+purpose+partner IBAN+EndToEndId for dedup
    public DateTime BookingDate { get; set; }
    public DateTime ValueDate { get; set; }
    public decimal Amount { get; set; }             // negative = expense, positive = income
    public string Currency { get; set; } = "EUR";
    public string Purpose { get; set; } = "";       // Verwendungszweck
    public string CreditorName { get; set; } = "";
    public string CreditorIban { get; set; } = "";
    public string DebtorName { get; set; } = "";
    public string DebtorIban { get; set; } = "";
    public int? CategoryId { get; set; }
    public Category? Category { get; set; }
    public bool IsRecurring { get; set; }
    public DateTime ImportedAt { get; set; } = DateTime.UtcNow;
}
