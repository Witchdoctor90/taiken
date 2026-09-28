using Microsoft.Extensions.Logging;
using Wolverine;

namespace Taiken.Messaging;

/// <summary>
/// Applied to every message: on failure, logs at <see cref="LogLevel.Error"/> with the message
/// type, <c>VenueId</c> (when the message is venue-scoped), <c>CorrelationId</c> and the
/// exception (ADR 0004). Logs on every failed attempt, not only the one that lands in the dead
/// letter queue — Wolverine's own dead-letter table is the authoritative record of that.
/// </summary>
public static class MessageLoggingMiddleware
{
    public static void Finally(Envelope envelope, ILogger logger, Exception? exception)
    {
        if (exception is null)
        {
            return;
        }

        var venueId = (envelope.Message as IVenueScopedMessage)?.VenueId;

        logger.LogError(
            exception,
            "Message {MessageType} failed (envelope {EnvelopeId}, attempt {Attempts}, venue {VenueId}, correlation {CorrelationId})",
            envelope.MessageType,
            envelope.Id,
            envelope.Attempts,
            venueId,
            envelope.CorrelationId);
    }
}
