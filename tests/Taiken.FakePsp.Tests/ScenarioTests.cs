using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Taiken.FakePsp;

namespace Taiken.FakePsp.Tests;

public sealed class ScenarioTests : IAsyncLifetime
{
    private readonly FakePspFactory _factory = new();
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
    public async Task Success_approves_and_sends_one_signed_webhook()
    {
        var session = await CreateSessionAsync("success-key", amountMajor: "850.00");

        var result = await RunScenario(session.Sid, "success");

        Assert.Equal("APR", result.St);
        var status = await GetStatus(session.Sid);
        Assert.Equal("APR", status.St);
        Assert.Equal("850.00", status.Paid);

        var deliveries = _factory.WebhookHandler.Requests;
        Assert.Single(deliveries);
        AssertValidSignature(deliveries[0]);
        var payload = JsonSerializer.Deserialize<WebhookPayloadDto>(deliveries[0].Body)!;
        Assert.Equal("APR", payload.St);
        Assert.Equal("850.00", payload.Paid);
    }

    [Fact]
    public async Task Decline_leaves_paid_at_zero()
    {
        var session = await CreateSessionAsync("decline-key", amountMajor: "500.00");

        var result = await RunScenario(session.Sid, "decline");

        Assert.Equal("DCL", result.St);
        var status = await GetStatus(session.Sid);
        Assert.Equal("0.00", status.Paid);
        Assert.Single(_factory.WebhookHandler.Requests);
    }

    [Fact]
    public async Task Timeout_sends_no_webhook_and_eventually_expires()
    {
        var session = await CreateSessionAsync("timeout-key", amountMajor: "100.00");

        var result = await RunScenario(session.Sid, "timeout");

        Assert.Equal("PND", result.St);
        Assert.Empty(_factory.WebhookHandler.Requests);

        var statusBeforeTtl = await GetStatus(session.Sid);
        Assert.Equal("PND", statusBeforeTtl.St);

        _factory.TimeProvider.Advance(TimeSpan.FromSeconds(session.Ttl + 1));

        var statusAfterTtl = await GetStatus(session.Sid);
        Assert.Equal("EXP", statusAfterTtl.St);
    }

    [Fact]
    public async Task Success_with_duplicate_webhook_sends_the_same_payload_twice()
    {
        var session = await CreateSessionAsync("dup-key", amountMajor: "300.00");

        await RunScenario(session.Sid, "success-duplicate-webhook");

        var deliveries = _factory.WebhookHandler.Requests;
        Assert.Equal(2, deliveries.Count);
        Assert.Equal(deliveries[0].Body, deliveries[1].Body);
    }

    [Fact]
    public async Task Success_with_different_amount_captures_less_than_requested()
    {
        var session = await CreateSessionAsync("diff-key", amountMajor: "300.00");

        await RunScenario(session.Sid, "success-different-amount");

        var status = await GetStatus(session.Sid);
        Assert.NotEqual("300.00", status.Paid);

        var payload = JsonSerializer.Deserialize<WebhookPayloadDto>(_factory.WebhookHandler.Requests[0].Body)!;
        Assert.Equal(status.Paid, payload.Paid);
    }

    [Fact]
    public async Task Unknown_scenario_is_rejected()
    {
        var session = await CreateSessionAsync("unknown-scenario-key", amountMajor: "10.00");

        var response = await _client.PostAsync($"/pay/{session.Sid}/scenario/not-a-real-scenario", content: null);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Pay_page_sets_frame_ancestors_to_the_sessions_embed_origin()
    {
        var session = await CreateSessionAsync("csp-key", amountMajor: "10.00");

        var response = await _client.GetAsync($"/pay/{session.Sid}");

        response.EnsureSuccessStatusCode();
        Assert.Equal("frame-ancestors https://app.test", response.Headers.GetValues("Content-Security-Policy").Single());
    }

    private static void AssertValidSignature(CapturedRequest request)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes("fake-psp-dev-secret"));
        var expected = Convert.ToHexStringLower(hmac.ComputeHash(Encoding.UTF8.GetBytes(request.Body)));
        Assert.Equal(expected, request.Signature);
    }

    private async Task<CreateSessionResponseDto> CreateSessionAsync(string idempotencyKey, string amountMajor)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/v2/sessions")
        {
            Content = JsonContent.Create(new
            {
                amount_major = amountMajor,
                ccy = "ALL",
                merchant = "demo",
                order_ref = "att_1",
                callback_url = "https://merchant.test/webhooks/psp",
                embed_origin = "https://app.test",
            }),
        };
        request.Headers.Add("Idempotency-Key", idempotencyKey);

        var response = await _client.SendAsync(request);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<CreateSessionResponseDto>())!;
    }

    private async Task<ScenarioResultDto> RunScenario(string sid, string scenario)
    {
        var response = await _client.PostAsync($"/pay/{sid}/scenario/{scenario}", content: null);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<ScenarioResultDto>())!;
    }

    private async Task<SessionStatusResponseDto> GetStatus(string sid)
    {
        var response = await _client.GetAsync($"/v2/sessions/{sid}");
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<SessionStatusResponseDto>())!;
    }
}
