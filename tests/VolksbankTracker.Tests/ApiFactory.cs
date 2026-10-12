using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace VolksbankTracker.Tests;

/// <summary>Hosts the real API in Development (no API key) with the given configuration overrides.</summary>
public sealed class ApiFactory(IReadOnlyDictionary<string, string> settings) : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        foreach (var (key, value) in settings)
            builder.UseSetting(key, value);
    }
}
