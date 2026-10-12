using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using VolksbankTracker.Core.Data;

namespace VolksbankTracker.Tests;

public sealed class SqliteToPostgresCopierTests(PostgresServerFixture server)
    : IClassFixture<PostgresServerFixture>, IAsyncLifetime
{
    private const string ProtectedCredentials = "CfDJ8-protected-payload";
    private readonly string _sqlitePath = Path.Combine(Path.GetTempPath(), $"vbt-copy-{Guid.NewGuid():N}.db");
    private AppDbContext _source = null!;
    private AppDbContext _target = null!;
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private static AppDbContext Create(DatabaseSettings settings)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>();
        options.UseAppDatabase(settings);
        return new AppDbContext(options.Options);
    }

    public async ValueTask InitializeAsync()
    {
        _source = Create(new DatabaseSettings(DatabaseProvider.Sqlite, $"Data Source={_sqlitePath}"));
        await _source.Database.MigrateAsync();
        await TestData.SeedAsync(_source);
        _source.Categories.Single(c => c.Id == 3).Name = "Lebensmittel (renamed)";
        _source.Categories.Add(new Category { Id = 42, Name = "Custom" });
        _source.MerchantCategoryMaps.Add(new MerchantCategoryMap { MatchKey = "DE01", CategoryId = 42 });
        await _source.SetSettingAsync("FinTs:Credentials", ProtectedCredentials);
        _source.ChangeTracker.Clear();

        _target = Create(new DatabaseSettings(DatabaseProvider.Postgres, await server.CreateDatabaseAsync()));
    }

    public async ValueTask DisposeAsync()
    {
        await _source.DisposeAsync();
        await _target.DisposeAsync();
        SqliteConnection.ClearAllPools();
        File.Delete(_sqlitePath);
    }

    [Fact]
    public async Task Copies_every_row_with_its_key_and_the_settings_verbatim()
    {
        var copied = await new SqliteToPostgresCopier(_source, _target).CopyAsync(Ct);

        Assert.Equal(await TableCounts.OfAsync(_source, Ct), copied);
        Assert.Equal("Lebensmittel (renamed)", (await _target.Categories.SingleAsync(c => c.Id == 3, Ct)).Name);
        Assert.Equal(42, (await _target.MerchantCategoryMaps.SingleAsync(Ct)).CategoryId);
        Assert.Equal(ProtectedCredentials, await _target.GetSettingAsync("FinTs:Credentials"));

        var expected = await _source.Transactions.AsNoTracking().OrderBy(t => t.Id).ToListAsync(Ct);
        var actual = await _target.Transactions.AsNoTracking().OrderBy(t => t.Id).ToListAsync(Ct);
        Assert.Equal(
            expected.Select(t => (t.Id, t.Hash, t.BookingDate, t.Amount, t.CategoryId, t.ImportedAt)),
            actual.Select(t => (t.Id, t.Hash, t.BookingDate, t.Amount, t.CategoryId, t.ImportedAt)));
    }

    [Fact]
    public async Task New_rows_after_the_copy_get_keys_past_the_copied_ones()
    {
        await new SqliteToPostgresCopier(_source, _target).CopyAsync(Ct);

        var category = new Category { Name = "After copy" };
        _target.Categories.Add(category);
        await _target.SaveChangesAsync(Ct);

        Assert.Equal(43, category.Id);
    }

    [Fact]
    public async Task Refuses_a_target_that_already_holds_data()
    {
        await new SqliteToPostgresCopier(_source, _target).CopyAsync(Ct);
        var before = await TableCounts.OfAsync(_target, Ct);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            new SqliteToPostgresCopier(_source, _target).CopyAsync(Ct));

        Assert.Equal(before, await TableCounts.OfAsync(_target, Ct));
    }
}
