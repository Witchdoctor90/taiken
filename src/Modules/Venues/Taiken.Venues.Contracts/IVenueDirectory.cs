using Taiken.Shared;

namespace Taiken.Venues.Contracts;

/// <summary>
/// Read-only venue directory: id/slug lookups that work without an <c>IVenueContext</c> scope
/// already set — this is how one gets set in the first place (table-QR resolving, webhook route,
/// ADR 0002). Never exposes secrets; see <see cref="IVenueCredentials"/> for those.
/// </summary>
public interface IVenueDirectory
{
    Task<VenueDirectoryEntry?> FindAsync(VenueId id, CancellationToken cancellationToken = default);

    Task<VenueDirectoryEntry?> FindBySlugAsync(string slug, CancellationToken cancellationToken = default);
}
