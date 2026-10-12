using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace VolksbankTracker.Tests;

/// <summary>Every provider must answer these identically; see SqliteApiTests and PostgresApiTests.</summary>
public abstract class ApiBehaviorTests(ProviderFixture fixture)
{
    private readonly HttpClient _client = fixture.Factory.CreateClient();
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private async Task<JsonElement> GetJsonAsync(string path)
    {
        var response = await _client.GetAsync(path, Ct);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return await response.Content.ReadFromJsonAsync<JsonElement>(Ct);
    }

    private async Task<string[]> SearchCreditorsAsync(string query) =>
        (await GetJsonAsync($"/api/transactions?search={Uri.EscapeDataString(query)}"))
            .GetProperty("items").EnumerateArray()
            .Select(t => t.GetProperty("creditorName").GetString()!)
            .Order()
            .ToArray();

    [Fact]
    public async Task Health_reports_healthy()
    {
        var response = await _client.GetAsync("/health", Ct);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Search_ignores_case()
    {
        Assert.Equal(["REWE Markt", "Rewe To Go"], await SearchCreditorsAsync("rewe"));
    }

    [Fact]
    public async Task Search_treats_percent_as_a_literal()
    {
        Assert.Equal(["Hofladen"], await SearchCreditorsAsync("%"));
    }

    [Fact]
    public async Task Sort_by_amount_orders_numerically()
    {
        var amounts = (await GetJsonAsync("/api/transactions?sortBy=amount&sortDir=asc"))
            .GetProperty("items").EnumerateArray()
            .Select(t => t.GetProperty("amount").GetDecimal())
            .ToArray();

        Assert.Equal([-1200.00m, -54.30m, -12.50m, -9.99m], amounts);
    }

    [Fact]
    public async Task Summary_aggregates_the_seeded_window()
    {
        var summary = await GetJsonAsync("/api/stats/summary");

        Assert.Equal(4, summary.GetProperty("transactionsInWindow").GetInt32());
        Assert.Equal(54.30m, summary.GetProperty("currentMonthExpenses").GetDecimal());
    }

    [Fact]
    public async Task Monthly_and_anomalies_run()
    {
        Assert.NotEmpty((await GetJsonAsync("/api/stats/monthly")).EnumerateArray());
        Assert.Equal(JsonValueKind.Array, (await GetJsonAsync("/api/stats/anomalies")).ValueKind);
    }

    [Fact]
    public async Task Booking_dates_round_trip_as_calendar_dates_without_offset()
    {
        var newest = (await GetJsonAsync("/api/transactions?sortBy=date&sortDir=desc"))
            .GetProperty("items")[0];

        Assert.Equal($"{TestData.Today:yyyy-MM-dd}T00:00:00", newest.GetProperty("bookingDate").GetString());
    }

    [Fact]
    public async Task Sync_timestamps_are_returned_as_utc()
    {
        var log = (await GetJsonAsync("/api/sync/logs"))[0];

        Assert.EndsWith("Z", log.GetProperty("startedAt").GetString());
        Assert.EndsWith("Z", log.GetProperty("completedAt").GetString());
    }

    [Fact]
    public async Task Created_category_gets_an_id_past_the_seeded_ones()
    {
        var response = await _client.PostAsJsonAsync("/api/categories", new { name = $"Test {Guid.NewGuid():N}" }, Ct);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<JsonElement>(Ct);
        Assert.True(created.GetProperty("id").GetInt32() > 9);
    }
}
