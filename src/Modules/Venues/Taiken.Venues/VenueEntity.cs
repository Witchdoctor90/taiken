using Taiken.Shared;
using Taiken.Venues.Contracts;

namespace Taiken.Venues;

/// <summary>
/// The <c>venues</c> table. Not venue-scoped itself (the property is <c>Id</c>, not <c>VenueId</c>
/// — ADR 0002's automatic Global Query Filter only matches a property literally named
/// <c>VenueId</c>): a venue is the unit of isolation, not data that belongs to one.
/// </summary>
internal sealed class VenueEntity
{
    public required VenueId Id { get; init; }

    public required string Slug { get; set; }

    public required string Name { get; set; }

    public required string Timezone { get; set; }

    public required Currency Currency { get; set; }

    public required VenueStatus Status { get; set; }

    public required string PosProvider { get; set; }

    public required string PspProvider { get; set; }

    public string? PosLocationRef { get; set; }

    public IReadOnlyDictionary<string, string> Config { get; set; } = new Dictionary<string, string>();

    public long ConfigVersion { get; set; }

    public required DateTimeOffset UpdatedAt { get; set; }

    public string? UpdatedBy { get; set; }
}
