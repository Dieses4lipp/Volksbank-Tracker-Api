using VolksbankTracker.Core.Data;

namespace VolksbankTracker.API;

/// <summary>
/// Reads DB_PROVIDER (sqlite|postgres, default sqlite) and the matching connection string.
/// SQLite uses ConnectionStrings:Default, Postgres uses ConnectionStrings:Postgres. They are
/// separate keys because the image sets a SQLite default for Default, which would otherwise
/// hide a forgotten Postgres connection string.
/// </summary>
public static class DatabaseConfiguration
{
    public const string ProviderKey = "DB_PROVIDER";

    public static DatabaseSettings Read(IConfiguration configuration)
    {
        var provider = configuration[ProviderKey]?.Trim().ToLowerInvariant() switch
        {
            null or "" or "sqlite" => DatabaseProvider.Sqlite,
            "postgres" => DatabaseProvider.Postgres,
            var unknown => throw new InvalidOperationException(
                $"{ProviderKey} must be 'sqlite' or 'postgres', got '{unknown}'.")
        };

        return provider switch
        {
            DatabaseProvider.Sqlite => new DatabaseSettings(provider,
                configuration.GetConnectionString("Default") ?? "Data Source=tracker.db"),
            _ => new DatabaseSettings(provider,
                configuration.GetConnectionString("Postgres") is { Length: > 0 } postgres
                    ? postgres
                    : throw new InvalidOperationException(
                        $"{ProviderKey}=postgres requires ConnectionStrings:Postgres (environment variable ConnectionStrings__Postgres)."))
        };
    }
}
