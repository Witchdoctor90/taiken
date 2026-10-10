using Taiken.Shared;
using Taiken.Venues.Contracts;

namespace Taiken.Venues;

/// <summary>
/// The <c>venue_secrets</c> table: a provider-agnostic key/value store for credentials, encrypted
/// at rest via <see cref="ICredentialProtector"/> (<c>PlaintextCredentialProtector</c> until
/// TAI-32). <c>VenueId</c> is venue-scoped data here on purpose — the automatic Global Query
/// Filter (ADR 0002) applies, so reading a venue's secrets requires the ambient
/// <c>IVenueContext</c> to already be scoped to that venue.
/// </summary>
internal sealed class VenueSecretEntity
{
    public required Guid Id { get; init; }

    public required VenueId VenueId { get; init; }

    public required CredentialScope Scope { get; init; }

    public required string Key { get; init; }

    public required byte[] ValueEnc { get; set; }
}
