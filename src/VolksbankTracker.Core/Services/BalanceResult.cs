namespace VolksbankTracker.Core.Services;

public sealed record BalanceResult : BankOperationResult
{
    public decimal? Balance { get; init; }
    public decimal? AvailableBalance { get; init; }
}
