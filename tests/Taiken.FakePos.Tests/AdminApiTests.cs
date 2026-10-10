using System.Net;
using System.Net.Http.Json;
using Taiken.FakePos;

namespace Taiken.FakePos.Tests;

public sealed class AdminApiTests : IAsyncLifetime
{
    private readonly FakePosFactory _factory = new();
    private HttpClient _client = null!;

    public Task InitializeAsync()
    {
        _client = _factory.CreateClient();
        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        _client.Dispose();
        await _factory.DisposeAsync();
    }

    [Fact]
    public async Task Locations_lists_the_seeded_demo_location()
    {
        var locations = await _client.GetFromJsonAsync<List<string>>("/admin/api/locations");

        Assert.Contains("demo", locations!);
    }

    [Fact]
    public async Task Tables_lists_each_tables_current_check()
    {
        var tables = await _client.GetFromJsonAsync<List<TableSummaryDto>>("/admin/api/locations/demo/tables");

        Assert.Equal(3, tables!.Count);
        Assert.All(tables, t => Assert.NotNull(t.Check));
    }

    [Fact]
    public async Task Adding_an_item_bumps_rev_and_recalculates_the_total()
    {
        var before = await _client.GetFromJsonAsync<TableSummaryDto>("/admin/api/locations/demo/tables/3");

        var response = await _client.PostAsJsonAsync(
            "/admin/api/locations/demo/tables/3/items",
            new { title = "Tea", qty = "2", price = "90.00" });

        response.EnsureSuccessStatusCode();
        var after = (await response.Content.ReadFromJsonAsync<CheckDto>())!;

        Assert.Equal(before!.Check!.Rev + 1, after.Rev);
        Assert.Equal("180.00", after.SumTotal);
    }

    [Fact]
    public async Task Resetting_retires_the_open_check_and_starts_a_fresh_one()
    {
        var before = await _client.GetFromJsonAsync<TableSummaryDto>("/admin/api/locations/demo/tables/1");

        var response = await _client.PostAsync("/admin/api/locations/demo/tables/1/reset", content: null);

        response.EnsureSuccessStatusCode();
        var fresh = (await response.Content.ReadFromJsonAsync<CheckDto>())!;
        Assert.NotEqual(before!.Check!.CheckId, fresh.CheckId);
        Assert.Empty(fresh.Items);
        Assert.Equal("0.00", fresh.SumTotal);

        var current = await _client.GetFromJsonAsync<TableSummaryDto>("/admin/api/locations/demo/tables/1");
        Assert.Equal(fresh.CheckId, current!.Check!.CheckId);
    }

    [Fact]
    public async Task Adding_an_item_when_the_check_is_already_paid_is_404()
    {
        var table = await _client.GetFromJsonAsync<TableSummaryDto>("/admin/api/locations/demo/tables/2");
        await _client.PostAsJsonAsync(
            $"/api/v1/checks/{table!.Check!.CheckId}/payments",
            new { amount = table.Check.SumTotal, ext_ref = "pi_close" });

        var response = await _client.PostAsJsonAsync(
            "/admin/api/locations/demo/tables/2/items",
            new { title = "Water", qty = "1", price = "20.00" });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
