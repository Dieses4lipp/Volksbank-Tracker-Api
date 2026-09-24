using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Logging;
using VolksbankTracker.Core.Data;

namespace VolksbankTracker.Core.Services;

/// <summary>
/// Stores the FinTS credentials encrypted (ASP.NET Core Data Protection) in the
/// AppSettings table. The API is the only source: they are submitted via
/// PUT /api/settings/fints, never read from configuration.
/// </summary>
public class FinTsCredentialsService(
    AppDbContext db,
    IDataProtectionProvider dataProtection,
    ILogger<FinTsCredentialsService> logger)
{
    public const string SettingKey = "FinTsCredentials";

    private readonly IDataProtector _protector =
        dataProtection.CreateProtector("VolksbankTracker.FinTsCredentials");

    public async Task<(FinTsConfig? Config, FinTsCredentialsSource Source)> GetAsync()
    {
        var encrypted = await db.GetSettingAsync(SettingKey);
        if (encrypted is null)
            return (null, FinTsCredentialsSource.None);

        try
        {
            var config = JsonSerializer.Deserialize<FinTsConfig>(_protector.Unprotect(encrypted));
            return config is null
                ? (null, FinTsCredentialsSource.Unreadable)
                : (config, FinTsCredentialsSource.Database);
        }
        catch (Exception ex) when (ex is CryptographicException or JsonException)
        {
            logger.LogWarning(ex,
                "AppSetting '{Key}' cannot be decrypted or parsed; re-submit the credentials via PUT /api/settings/fints.",
                SettingKey);
            return (null, FinTsCredentialsSource.Unreadable);
        }
    }

    public Task SaveAsync(FinTsConfig config) =>
        db.SetSettingAsync(SettingKey, _protector.Protect(JsonSerializer.Serialize(config)));

    public Task DeleteAsync() =>
        db.RemoveSettingAsync(SettingKey);
}
