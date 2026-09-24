using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using VolksbankTracker.Core.Data;

namespace VolksbankTracker.Core.Services;

/// <summary>
/// Stores the transaction-classification lists (savings IBANs, salary debtors, ...)
/// in the AppSettings table. The API is the only source: they are set via
/// PUT /api/settings/classification and start out empty.
/// </summary>
public class ClassificationSettingsService(
    AppDbContext db,
    ILogger<ClassificationSettingsService> logger)
{
    public const string SettingKey = "FinTsClassification";

    private static readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    public async Task<ClassificationSettings> GetAsync()
    {
        var json = await db.GetSettingAsync(SettingKey);
        if (json is null)
            return ClassificationSettings.Empty;

        try
        {
            return JsonSerializer.Deserialize<ClassificationSettings>(json, _jsonOptions)
                   ?? ClassificationSettings.Empty;
        }
        catch (JsonException ex)
        {
            logger.LogWarning(ex,
                "AppSetting '{Key}' contains invalid JSON; falling back to empty classification settings.",
                SettingKey);
            return ClassificationSettings.Empty;
        }
    }

    public async Task<ClassificationSettings> SaveAsync(ClassificationSettings settings)
    {
        settings = settings.Normalized();
        await db.SetSettingAsync(SettingKey, JsonSerializer.Serialize(settings, _jsonOptions));
        return settings;
    }
}
