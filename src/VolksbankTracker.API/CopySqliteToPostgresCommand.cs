using Microsoft.EntityFrameworkCore;
using VolksbankTracker.Core.Data;

namespace VolksbankTracker.API;

/// <summary>
/// `dotnet VolksbankTracker.API.dll copy-sqlite-to-postgres` copies ConnectionStrings:Default
/// (SQLite) into ConnectionStrings:Postgres, ignoring DB_PROVIDER. Stop the API first so
/// nothing writes to SQLite during the copy.
/// </summary>
public static class CopySqliteToPostgresCommand
{
    public const string Name = "copy-sqlite-to-postgres";

    public static async Task<int> RunAsync(IConfiguration configuration)
    {
        await using var source = Create(DatabaseConfiguration.ReadSqlite(configuration));
        await using var target = Create(DatabaseConfiguration.ReadPostgres(configuration));

        try
        {
            var copied = await new SqliteToPostgresCopier(source, target).CopyAsync();
            Console.WriteLine($"Copied {copied}.");
            return 0;
        }
        catch (InvalidOperationException e)
        {
            Console.Error.WriteLine($"Copy failed, no rows written:{e.Message}");
            return 1;
        }
    }

    private static AppDbContext Create(DatabaseSettings settings)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>();
        options.UseAppDatabase(settings);
        return new AppDbContext(options.Options);
    }
}
