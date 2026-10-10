using System.Net;
using System.Net.Http.Json;
using Taiken.FakePos;

namespace Taiken.FakePos.Tests;

public sealed class PublicApiTests : IAsyncLifetime
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
    public async Task Seeded_demo_table_1_has_the_expected_total()
    {
        var check = await GetCheck("demo", "1");

        Assert.Equal("OPEN", check.State);
        Assert.Equal("370.00", check.SumTotal);
        Assert.Equal(2, check.Items.Count);
        Assert.Equal("L1", check.Items[0].Line);
        Assert.Single(check.Items[0].Mods);
    }

    [Fact]
    public async Task Unknown_table_is_404()
    {
        var response = await _client.GetAsync("/api/v1/locations/demo/tables/does-not-exist/check");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Payment_closes_the_check()
    {
        var check = await GetCheck("demo", "2");

        var response = await Pay(check.CheckId, check.SumTotal, "pi_1");

        response.EnsureSuccessStatusCode();
        var paid = (await response.Content.ReadFromJsonAsync<CheckDto>())!;
        Assert.Equal("PAID", paid.State);
    }

    [Fact]
    public async Task Replaying_the_same_ext_ref_is_idempotent()
    {
        var check = await GetCheck("demo", "2");
        await Pay(check.CheckId, check.SumTotal, "pi_dup");

        var second = await Pay(check.CheckId, check.SumTotal, "pi_dup");

        second.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task A_different_ext_ref_on_an_already_paid_check_is_a_conflict()
    {
        var check = await GetCheck("demo", "2");
        await Pay(check.CheckId, check.SumTotal, "pi_first");

        var conflicting = await Pay(check.CheckId, check.SumTotal, "pi_second");

        Assert.Equal(HttpStatusCode.Conflict, conflicting.StatusCode);
    }

    [Fact]
    public async Task Paying_an_unknown_check_is_404()
    {
        var response = await Pay("CHK-999999", "10.00", "pi_x");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private async Task<CheckDto> GetCheck(string loc, string tableNo)
    {
        var response = await _client.GetAsync($"/api/v1/locations/{loc}/tables/{tableNo}/check");
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<CheckDto>())!;
    }

    private async Task<HttpResponseMessage> Pay(string checkId, string amount, string extRef) =>
        await _client.PostAsJsonAsync($"/api/v1/checks/{checkId}/payments", new { amount, ext_ref = extRef });
}
