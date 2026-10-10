using System.Net;
using System.Net.Http.Json;
using Taiken.FakePsp;

namespace Taiken.FakePsp.Tests;

public sealed class SessionLifecycleTests : IAsyncLifetime
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
    public async Task Missing_idempotency_key_is_rejected()
    {
        var response = await PostSessionRaw(withIdempotencyKey: false);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Same_idempotency_key_returns_the_same_session()
    {
        var first = await CreateSession("key-1");
        var second = await CreateSession("key-1");

        Assert.Equal(first.Sid, second.Sid);
    }

    [Fact]
    public async Task Different_idempotency_keys_create_different_sessions()
    {
        var first = await CreateSession("key-a");
        var second = await CreateSession("key-b");

        Assert.NotEqual(first.Sid, second.Sid);
    }

    [Fact]
    public async Task Unknown_session_status_is_404()
    {
        var response = await _client.GetAsync("/v2/sessions/S-does-not-exist");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task New_session_is_pending_with_nothing_paid()
    {
        var created = await CreateSession("key-pending");

        var status = await GetStatus(created.Sid);

        Assert.Equal("PND", status.St);
        Assert.Equal("0.00", status.Paid);
    }

    [Theory]
    [InlineData("not-a-number")]
    [InlineData("-5.00")]
    public async Task Invalid_amount_is_rejected(string amountMajor)
    {
        var response = await PostSessionRaw(withIdempotencyKey: true, amountMajor: amountMajor);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private async Task<HttpResponseMessage> PostSessionRaw(
        bool withIdempotencyKey,
        string idempotencyKey = "default-key",
        string amountMajor = "850.00")
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

        if (withIdempotencyKey)
        {
            request.Headers.Add("Idempotency-Key", idempotencyKey);
        }

        return await _client.SendAsync(request);
    }

    private async Task<CreateSessionResponseDto> CreateSession(string idempotencyKey)
    {
        var response = await PostSessionRaw(withIdempotencyKey: true, idempotencyKey);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<CreateSessionResponseDto>())!;
    }

    private async Task<SessionStatusResponseDto> GetStatus(string sid)
    {
        var response = await _client.GetAsync($"/v2/sessions/{sid}");
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<SessionStatusResponseDto>())!;
    }
}
