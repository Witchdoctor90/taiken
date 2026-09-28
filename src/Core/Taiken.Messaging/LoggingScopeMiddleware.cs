using Microsoft.Extensions.Logging;
using Wolverine;

namespace Taiken.Messaging;

/// <summary>
/// Applied to every message: puts <c>CorrelationId</c>, <c>VenueId</c> (when the message is
/// venue-scoped) and the message type into the logging scope for the duration of the handler, so
/// every log line written while processing a message carries them (ADR 0004: Observability).
/// </summary>
public static class LoggingScopeMiddleware
{
    public static LoggingScope Before(Envelope envelope, ILogger logger)
    {
        var venueId = (envelope.Message as IVenueScopedMessage)?.VenueId;

        var scope = logger.BeginScope(new Dictionary<string, object?>
        {
            ["CorrelationId"] = envelope.CorrelationId,
            ["VenueId"] = venueId,
            ["MessageType"] = envelope.MessageType,
        });

        return new LoggingScope(scope);
    }

    public static void Finally(LoggingScope scope) => scope.Dispose();
}

/// <summary>Wraps the <see cref="ILogger.BeginScope{TState}"/> handle Wolverine passes from <see cref="LoggingScopeMiddleware.Before"/> into <see cref="LoggingScopeMiddleware.Finally"/>.</summary>
public sealed class LoggingScope(IDisposable? scope) : IDisposable
{
    public void Dispose() => scope?.Dispose();
}
