using Taiken.Shared;

namespace Taiken.Persistence;

public sealed class VenueContext : IVenueContext
{
    private Scope? _scope;

    public bool IsSystem => _scope is { IsSystem: true };

    public VenueId VenueId => _scope switch
    {
        { IsSystem: false } scope => scope.VenueId!,
        { IsSystem: true } => throw new InvalidOperationException(
            "The venue context is in system mode and has no single VenueId."),
        null => throw new InvalidOperationException(
            "The venue context has not been set. Venue-scoped data cannot be accessed without an explicit scope."),
    };

    public VenueId? VenueIdOrNull => _scope switch
    {
        { IsSystem: false } scope => scope.VenueId,
        { IsSystem: true } => null,
        null => throw new InvalidOperationException(
            "The venue context has not been set. Venue-scoped data cannot be accessed without an explicit scope."),
    };

    public void Enter(VenueId venueId) => _scope = new Scope(IsSystem: false, venueId);

    public void EnterSystem() => _scope = new Scope(IsSystem: true, VenueId: null);

    private sealed record Scope(bool IsSystem, VenueId? VenueId);
}
