using System.ComponentModel.DataAnnotations;
using VolksbankTracker.Core.Services;

namespace VolksbankTracker.API.Models;

public record FinTsCredentialsRequest(
    [Required, Url] string BankUrl,
    [Required, RegularExpression(@"^\d{8}$", ErrorMessage = "BlZ must be 8 digits.")] string BlZ,
    [Required, StringLength(34, MinimumLength = 15)] string Iban,
    [Required] string UserId,
    [Required] string Pin,
    [StringLength(11)] string Bic = "",
    string Account = "")
{
    public FinTsConfig ToConfig() => new()
    {
        BankUrl = BankUrl.Trim(),
        BlZ = BlZ,
        Iban = Iban.Replace(" ", "").ToUpperInvariant(),
        Bic = Bic.Trim().ToUpperInvariant(),
        Account = Account.Trim(),
        UserId = UserId.Trim(),
        Pin = Pin
    };
}
