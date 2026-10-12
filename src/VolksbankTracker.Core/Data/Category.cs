namespace VolksbankTracker.Core.Data;

public class Category
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Icon { get; set; } = "";
    public string Color { get; set; } = "#6b7280";
    public List<Transaction> Transactions { get; set; } = [];
    /// <summary>
    /// Transactions matching no merchant mapping land here.
    /// </summary>
    public bool IsFallback { get; set; }
    /// <summary>
    /// Transactions classified as savings (see ClassificationSettings) always land here.
    /// </summary>
    public bool IsSavings { get; set; }
}
