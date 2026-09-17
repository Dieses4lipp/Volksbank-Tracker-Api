using VolksbankTracker.Core.Data;

namespace VolksbankTracker.Core.Services;

/// <summary>Common outcome of an operation that talks to the bank.</summary>
public abstract record BankOperationResult
{
    public string Status { get; init; } = SyncStatus.Success;
    public string? Error { get; init; }

    public bool Succeeded => Status == SyncStatus.Success;
}
