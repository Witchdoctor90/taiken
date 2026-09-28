using Taiken.Persistence;

namespace Taiken.Messaging;

/// <summary>
/// Applied to every <see cref="IVenueScopedMessage"/> handler chain: sets the ambient
/// <see cref="IVenueContext"/> from the message before the handler runs (ADR 0002).
/// </summary>
public static class VenueContextMiddleware
{
    public static void Before(IVenueScopedMessage message, IVenueContext venueContext) =>
        venueContext.Enter(message.VenueId);
}
