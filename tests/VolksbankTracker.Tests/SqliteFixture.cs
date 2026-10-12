using Microsoft.Data.Sqlite;

namespace VolksbankTracker.Tests;

public sealed class SqliteFixture : ProviderFixture
{
    private readonly string _path = Path.Combine(Path.GetTempPath(), $"vbt-test-{Guid.NewGuid():N}.db");

    protected override Task<IReadOnlyDictionary<string, string>> StartDatabaseAsync() =>
        Task.FromResult<IReadOnlyDictionary<string, string>>(new Dictionary<string, string>
        {
            ["DB_PROVIDER"] = "sqlite",
            ["ConnectionStrings:Default"] = $"Data Source={_path}"
        });

    protected override ValueTask StopDatabaseAsync()
    {
        SqliteConnection.ClearAllPools();
        File.Delete(_path);
        return ValueTask.CompletedTask;
    }
}
