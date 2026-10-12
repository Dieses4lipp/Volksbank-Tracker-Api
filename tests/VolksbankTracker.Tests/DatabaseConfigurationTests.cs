using Microsoft.Extensions.Configuration;
using VolksbankTracker.API;
using VolksbankTracker.Core.Data;

namespace VolksbankTracker.Tests;

public class DatabaseConfigurationTests
{
    private static IConfiguration Config(params (string Key, string Value)[] values) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(values.Select(v => new KeyValuePair<string, string?>(v.Key, v.Value)))
            .Build();

    [Fact]
    public void Defaults_to_sqlite_with_the_default_connection_string()
    {
        var settings = DatabaseConfiguration.Read(Config(("ConnectionStrings:Default", "Data Source=x.db")));

        Assert.Equal(new DatabaseSettings(DatabaseProvider.Sqlite, "Data Source=x.db"), settings);
    }

    [Theory]
    [InlineData("postgres")]
    [InlineData(" Postgres ")]
    public void Selects_postgres_with_its_own_connection_string(string provider)
    {
        var settings = DatabaseConfiguration.Read(Config(
            ("DB_PROVIDER", provider),
            ("ConnectionStrings:Default", "Data Source=x.db"),
            ("ConnectionStrings:Postgres", "Host=db")));

        Assert.Equal(new DatabaseSettings(DatabaseProvider.Postgres, "Host=db"), settings);
    }

    [Fact]
    public void Rejects_an_unknown_provider()
    {
        var e = Assert.Throws<InvalidOperationException>(() =>
            DatabaseConfiguration.Read(Config(("DB_PROVIDER", "mysql"))));

        Assert.Contains("'mysql'", e.Message);
    }

    [Fact]
    public void Rejects_postgres_without_a_postgres_connection_string_even_when_default_is_set()
    {
        Assert.Throws<InvalidOperationException>(() => DatabaseConfiguration.Read(Config(
            ("DB_PROVIDER", "postgres"),
            ("ConnectionStrings:Default", "Data Source=x.db"))));
    }
}
