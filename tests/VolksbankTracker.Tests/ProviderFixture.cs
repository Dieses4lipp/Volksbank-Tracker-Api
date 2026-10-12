using Microsoft.Extensions.DependencyInjection;
using VolksbankTracker.Core.Data;

namespace VolksbankTracker.Tests;

/// <summary>Starts the API against one provider, which applies its migrations, then seeds <see cref="TestData"/>.</summary>
public abstract class ProviderFixture : IAsyncLifetime
{
    public ApiFactory Factory { get; private set; } = null!;

    protected abstract Task<IReadOnlyDictionary<string, string>> StartDatabaseAsync();

    protected virtual ValueTask StopDatabaseAsync() => ValueTask.CompletedTask;

    public async ValueTask InitializeAsync()
    {
        Factory = new ApiFactory(await StartDatabaseAsync());

        using var scope = Factory.Services.CreateScope();
        await TestData.SeedAsync(scope.ServiceProvider.GetRequiredService<AppDbContext>());
    }

    public async ValueTask DisposeAsync()
    {
        await Factory.DisposeAsync();
        await StopDatabaseAsync();
    }
}
