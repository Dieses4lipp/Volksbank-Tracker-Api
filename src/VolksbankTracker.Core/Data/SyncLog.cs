namespace VolksbankTracker.Core.Data;

public class SyncLog
{
    public int Id { get; set; }
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
    public int TransactionsFetched { get; set; }
    public int TransactionsNew { get; set; }
    public string Status { get; set; } = SyncStatus.Running; // see SyncStatus
    public string? Error { get; set; }
}
