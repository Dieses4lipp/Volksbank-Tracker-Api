using VolksbankTracker.Core.Data;

namespace VolksbankTracker.Core.Services;

public sealed record BalanceResult
{
    public string Status { get; init; } = SyncStatus.Success;
    public string? Error { get; init; }
    public decimal? Balance { get; init; }
    public decimal? AvailableBalance { get; init; }

    public bool Succeeded => Status == SyncStatus.Success;
}
