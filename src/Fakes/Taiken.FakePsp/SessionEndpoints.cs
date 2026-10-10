using System.Globalization;
using Microsoft.Extensions.Options;

namespace Taiken.FakePsp;

internal static class SessionEndpoints
{
    public static IResult CreateSession(
        HttpRequest request,
        CreateSessionRequestDto body,
        SessionStore store,
        IOptions<FakePspOptions> options,
        TimeProvider timeProvider)
    {
        if (!request.Headers.TryGetValue("Idempotency-Key", out var keys) || string.IsNullOrWhiteSpace(keys.ToString()))
        {
            return Results.BadRequest(new ErrorDto("Idempotency-Key header is required."));
        }

        if (!TryValidate(body, out var validationError))
        {
            return Results.BadRequest(new ErrorDto(validationError!));
        }

        var session = store.CreateOrGetExisting(keys.ToString(), sid => new Session
        {
            Sid = sid,
            AmountMajor = body.AmountMajor,
            Currency = body.Ccy,
            Merchant = body.Merchant,
            OrderRef = body.OrderRef,
            CallbackUrl = body.CallbackUrl,
            EmbedOrigin = body.EmbedOrigin,
            Ttl = options.Value.DefaultTtlSeconds,
            CreatedAt = timeProvider.GetUtcNow(),
        });

        var payUrl = $"{request.Scheme}://{request.Host}/pay/{session.Sid}";
        return Results.Ok(new CreateSessionResponseDto(session.Sid, payUrl, session.Ttl));
    }

    public static IResult GetSession(string sid, SessionStore store, TimeProvider timeProvider)
    {
        var session = store.Find(sid);
        if (session is null)
        {
            return Results.NotFound();
        }

        var status = SessionExpiry.EffectiveStatus(session, timeProvider);
        return Results.Ok(new SessionStatusResponseDto(session.Sid, SessionStatusCodes.ToCode(status), session.Paid));
    }

    private static bool TryValidate(CreateSessionRequestDto body, out string? error)
    {
        if (!decimal.TryParse(body.AmountMajor, NumberStyles.Number, CultureInfo.InvariantCulture, out var amount) || amount < 0)
        {
            error = "amount_major must be a non-negative decimal string.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(body.Ccy))
        {
            error = "ccy is required.";
            return false;
        }

        if (!Uri.TryCreate(body.CallbackUrl, UriKind.Absolute, out _))
        {
            error = "callback_url must be an absolute URL.";
            return false;
        }

        if (!Uri.TryCreate(body.EmbedOrigin, UriKind.Absolute, out _))
        {
            error = "embed_origin must be an absolute URL.";
            return false;
        }

        error = null;
        return true;
    }
}
