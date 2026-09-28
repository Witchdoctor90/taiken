using Taiken.Shared;

namespace Taiken.Messaging;

/// <summary>
/// A command or integration event that carries the venue it belongs to. Wolverine middleware
/// (<see cref="VenueContextMiddleware"/>) sets the venue context from this before the handler
/// runs (ADR 0002).
/// </summary>
public interface IVenueScopedMessage
{
    VenueId VenueId { get; }
}
