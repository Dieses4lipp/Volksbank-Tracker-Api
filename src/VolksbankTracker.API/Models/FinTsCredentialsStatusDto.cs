using VolksbankTracker.Core.Services;

namespace VolksbankTracker.API.Models;

/// <summary>Credential status without secrets: the PIN is never returned, identifiers are masked.</summary>
public record FinTsCredentialsStatusDto(
    FinTsCredentialsSource Source,
    string BankUrl,
    string BlZ,
    string Bic,
    string IbanMasked,
    string AccountMasked,
    string UserIdMasked,
    bool HasPin)
{
    public static FinTsCredentialsStatusDto From(FinTsConfig? config, FinTsCredentialsSource source) =>
        config is null
            ? new(source, "", "", "", "", "", "", false)
            : new(source,
                config.BankUrl,
                config.BlZ,
                config.Bic,
                Mask(config.Iban),
                Mask(config.Account),
                Mask(config.UserId),
                !string.IsNullOrEmpty(config.Pin));

    /// <summary>Keeps the last 4 characters; shorter values are masked completely.</summary>
    private static string Mask(string value) =>
        value.Length <= 4
            ? new string('*', value.Length)
            : new string('*', value.Length - 4) + value[^4..];
}
