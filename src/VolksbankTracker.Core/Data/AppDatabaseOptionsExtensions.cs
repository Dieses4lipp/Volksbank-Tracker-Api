using Microsoft.EntityFrameworkCore;

namespace VolksbankTracker.Core.Data;

public static class AppDatabaseOptionsExtensions
{
    // Each provider has its own migration set: column types and schema differ.
    private const string SqliteMigrationsAssembly = "VolksbankTracker.Migrations.Sqlite";
    private const string PostgresMigrationsAssembly = "VolksbankTracker.Migrations.Postgres";

    public static DbContextOptionsBuilder UseAppDatabase(this DbContextOptionsBuilder options, DatabaseSettings settings) =>
        settings.Provider switch
        {
            DatabaseProvider.Sqlite => options.UseSqlite(settings.ConnectionString,
                sqlite => sqlite.MigrationsAssembly(SqliteMigrationsAssembly)),
            DatabaseProvider.Postgres => options.UseNpgsql(settings.ConnectionString,
                npgsql => npgsql
                    .MigrationsAssembly(PostgresMigrationsAssembly)
                    .MigrationsHistoryTable("__EFMigrationsHistory", AppDbContext.PostgresSchema)),
            _ => throw new ArgumentOutOfRangeException(nameof(settings), settings.Provider, null)
        };
}
