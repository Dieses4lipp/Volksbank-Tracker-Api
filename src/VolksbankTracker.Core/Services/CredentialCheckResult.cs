namespace VolksbankTracker.Core.Services;

public sealed record CredentialCheckResult : BankOperationResult
{
    /// <summary>True when the bank answered with an error (e.g. wrong PIN), false for connection/setup failures.</summary>
    public bool Rejected { get; init; }
}
