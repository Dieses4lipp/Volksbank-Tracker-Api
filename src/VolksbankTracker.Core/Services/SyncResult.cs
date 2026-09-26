namespace VolksbankTracker.Core.Services;

public sealed record SyncResult : BankOperationResult
{
    public int Fetched { get; init; }
    public int NewRecords { get; init; }
}
