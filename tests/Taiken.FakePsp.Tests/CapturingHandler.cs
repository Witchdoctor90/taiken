using System.Net;

namespace Taiken.FakePsp.Tests;

/// <summary>Stands in for the merchant's webhook receiver: captures every delivery instead of making a real HTTP call.</summary>
internal sealed class CapturingHandler : DelegatingHandler
{
    private readonly List<CapturedRequest> _requests = [];

    public IReadOnlyList<CapturedRequest> Requests
    {
        get
        {
            lock (_requests)
            {
                return [.. _requests];
            }
        }
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var body = request.Content is null ? string.Empty : await request.Content.ReadAsStringAsync(cancellationToken);
        var signature = request.Headers.TryGetValues("X-Signature", out var values) ? values.FirstOrDefault() : null;

        lock (_requests)
        {
            _requests.Add(new CapturedRequest(request.RequestUri!, body, signature));
        }

        return new HttpResponseMessage(HttpStatusCode.OK);
    }
}

internal sealed record CapturedRequest(Uri Uri, string Body, string? Signature);
