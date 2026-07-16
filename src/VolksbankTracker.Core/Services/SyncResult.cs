using VolksbankTracker.Core.Data;

namespace VolksbankTracker.Core.Services;

public sealed record SyncResult
{
    public int Fetched { get; init; }
    public int NewRecords { get; init; }
    public string Status { get; init; } = SyncStatus.Success;
    public string? Error { get; init; }

    public bool Succeeded => Status == SyncStatus.Success;
}
