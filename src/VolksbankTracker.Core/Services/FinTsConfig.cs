namespace VolksbankTracker.Core.Services;

/// <summary>Connection settings for the bank's FinTS endpoint ("FinTs" configuration section).</summary>
public sealed record FinTsConfig
{
    public string BankUrl { get; init; } = "";
    public string BlZ { get; init; } = "";
    public string Iban { get; init; } = "";
    public string Bic { get; init; } = "";
    public string Account { get; init; } = "";
    public string UserId { get; init; } = "";
    public string Pin { get; init; } = "";

    public bool IsComplete =>
        !string.IsNullOrWhiteSpace(BankUrl) &&
        !string.IsNullOrWhiteSpace(BlZ) &&
        !string.IsNullOrWhiteSpace(Iban);
}
