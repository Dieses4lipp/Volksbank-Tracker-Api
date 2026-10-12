using Testcontainers.PostgreSql;

namespace VolksbankTracker.Tests;

public sealed class PostgresFixture : ProviderFixture
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:17-alpine").Build();

    protected override async Task<IReadOnlyDictionary<string, string>> StartDatabaseAsync()
    {
        await _container.StartAsync();
        return new Dictionary<string, string>
        {
            ["DB_PROVIDER"] = "postgres",
            ["ConnectionStrings:Postgres"] = _container.GetConnectionString()
        };
    }

    protected override ValueTask StopDatabaseAsync() => _container.DisposeAsync();
}
