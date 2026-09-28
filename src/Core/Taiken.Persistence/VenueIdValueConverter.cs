using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Taiken.Shared;

namespace Taiken.Persistence;

/// <summary>Maps <see cref="VenueId"/> to and from its underlying <see cref="Guid"/> column value.</summary>
public sealed class VenueIdValueConverter : ValueConverter<VenueId, Guid>
{
    public VenueIdValueConverter()
        : base(venueId => venueId.Value, value => new VenueId(value))
    {
    }
}
