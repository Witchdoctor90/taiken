using Taiken.Shared;

namespace Taiken.Shared.Tests;

public class StronglyTypedIdTests
{
    [Fact]
    public void Same_type_and_value_are_equal()
    {
        var value = Guid.NewGuid();

        Assert.Equal(new VenueId(value), new VenueId(value));
    }

    [Fact]
    public void Different_id_types_with_the_same_guid_are_not_equal()
    {
        var value = Guid.NewGuid();

        StronglyTypedId venueId = new VenueId(value);
        StronglyTypedId customerId = new CustomerId(value);

        Assert.False(venueId.Equals(customerId));
    }

    [Fact]
    public void New_generates_distinct_ids()
    {
        Assert.NotEqual(VenueId.New(), VenueId.New());
    }
}
