using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using VolksbankTracker.Core.Data;

namespace VolksbankTracker.Core.Services;

public record ClassificationSettings(
    List<string> SavingsIbans,
    List<string> SavingsCreditorNames,
    List<string> SalaryDebtorNames,
    List<string> CashDepositKeywords,
    SalaryMonthConvention SalaryConvention = SalaryMonthConvention.PreviousMonth)
{
    public static ClassificationSettings Empty => new([], [], [], []);

    public ClassificationSettings Normalized() => new(
        Clean(SavingsIbans),
        Clean(SavingsCreditorNames),
        Clean(SalaryDebtorNames),
        Clean(CashDepositKeywords),
        SalaryConvention);

    private static List<string> Clean(List<string>? items) =>
        (items ?? [])
            .Select(i => i.Trim())
            .Where(i => i.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
}

/// <summary>
/// Stores the transaction-classification lists (savings IBANs, salary debtors, ...)
/// in the AppSettings table. On first access the values are seeded from the
/// FinTsClassification configuration section (user secrets / appsettings).
/// </summary>
public class ClassificationSettingsService(
    AppDbContext db,
    IConfiguration config,
    ILogger<ClassificationSettingsService> logger)
{
    public const string SettingKey = "FinTsClassification";

    private static readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    public async Task<ClassificationSettings> GetAsync()
    {
        var row = await db.AppSettings.FindAsync(SettingKey);
        if (row is null)
            return await SaveAsync(ReadFromConfiguration());

        try
        {
            return JsonSerializer.Deserialize<ClassificationSettings>(row.Value, _jsonOptions)
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
        var json = JsonSerializer.Serialize(settings, _jsonOptions);

        var row = await db.AppSettings.FindAsync(SettingKey);
        if (row is null)
            db.AppSettings.Add(new AppSetting { Key = SettingKey, Value = json });
        else
            row.Value = json;

        await db.SaveChangesAsync();
        return settings;
    }

    private ClassificationSettings ReadFromConfiguration() => new ClassificationSettings(
        GetList("SavingsIbans"),
        GetList("SavingsCreditorNames"),
        GetList("SalaryDebtorNames"),
        GetList("CashDepositKeywords"),
        config.GetSection($"{SettingKey}:SalaryConvention").Get<SalaryMonthConvention?>()
            ?? SalaryMonthConvention.PreviousMonth).Normalized();

    private List<string> GetList(string name) =>
        config.GetSection($"{SettingKey}:{name}").Get<List<string>>() ?? [];
}
