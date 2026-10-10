using Microsoft.EntityFrameworkCore;
using Taiken.Shared;
using Taiken.Venues.Contracts;

namespace Taiken.Venues;

/// <summary>
/// Reads <see cref="VenueEntity"/> directly: unfiltered by venue (ADR 0002 — a venue is the unit
/// of isolation, not venue-scoped data), so lookups work before any <c>IVenueContext</c> scope is
/// set, which is exactly when table-QR resolving needs them.
/// </summary>
internal sealed class VenueDirectory(VenuesDbContext db) : IVenueDirectory
{
    public async Task<VenueDirectoryEntry?> FindAsync(VenueId id, CancellationToken cancellationToken = default)
    {
        var entity = await db.Venues.SingleOrDefaultAsync(v => v.Id == id, cancellationToken);
        return entity is null ? null : ToDirectoryEntry(entity);
    }

    public async Task<VenueDirectoryEntry?> FindBySlugAsync(string slug, CancellationToken cancellationToken = default)
    {
        var entity = await db.Venues.SingleOrDefaultAsync(v => v.Slug == slug, cancellationToken);
        return entity is null ? null : ToDirectoryEntry(entity);
    }

    private static VenueDirectoryEntry ToDirectoryEntry(VenueEntity entity) => new(
        entity.Id,
        entity.Slug,
        entity.Name,
        entity.Timezone,
        entity.Currency,
        entity.Status,
        entity.PosProvider,
        entity.PspProvider,
        entity.Config,
        entity.ConfigVersion);
}
