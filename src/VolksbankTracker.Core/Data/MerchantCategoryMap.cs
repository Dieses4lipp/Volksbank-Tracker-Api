namespace VolksbankTracker.Core.Data;

public class MerchantCategoryMap
{
    public int Id { get; set; }
    public string MatchKey { get; set; } = ""; // IBAN, or normalized name if no IBAN
    public int CategoryId { get; set; }
    public Category Category { get; set; } = null!;
}
