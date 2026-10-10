using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace Taiken.FakePsp;

internal sealed class WebhookSender(
    IHttpClientFactory httpClientFactory,
    IOptions<FakePspOptions> options,
    TimeProvider timeProvider,
    ILogger<WebhookSender> logger)
{
    public const string HttpClientName = "fakepsp-webhook";

    public async Task SendAsync(Session session, CancellationToken cancellationToken)
    {
        var delay = options.Value.WebhookDelay;
        if (delay > TimeSpan.Zero)
        {
            await Task.Delay(delay, timeProvider, cancellationToken);
        }

        var payload = new WebhookPayloadDto(
            session.Sid,
            SessionStatusCodes.ToCode(session.Status),
            session.Paid,
            timeProvider.GetUtcNow().ToUnixTimeSeconds());
        var json = JsonSerializer.Serialize(payload);

        using var client = httpClientFactory.CreateClient(HttpClientName);
        using var request = new HttpRequestMessage(HttpMethod.Post, session.CallbackUrl)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json"),
        };
        request.Headers.Add("X-Signature", Sign(json, options.Value.WebhookSecret));

        try
        {
            using var response = await client.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning(
                    "Webhook to {CallbackUrl} for session {Sid} returned {StatusCode}.",
                    session.CallbackUrl, session.Sid, (int)response.StatusCode);
            }
        }
        catch (HttpRequestException ex)
        {
            logger.LogWarning(ex, "Webhook delivery to {CallbackUrl} for session {Sid} failed.", session.CallbackUrl, session.Sid);
        }
    }

    private static string Sign(string json, string secret)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(json));
        return Convert.ToHexStringLower(hash);
    }
}
