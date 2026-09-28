using Taiken.Shared;

namespace Taiken.Persistence.Tests;

public class VenueContextTests
{
    [Fact]
    public void VenueId_throws_when_no_scope_is_set()
    {
        IVenueContext context = new VenueContext();

        Assert.Throws<InvalidOperationException>(() => context.VenueId);
    }

    [Fact]
    public void VenueId_throws_in_system_mode()
    {
        IVenueContext context = new VenueContext();
        context.EnterSystem();

        Assert.True(context.IsSystem);
        Assert.Throws<InvalidOperationException>(() => context.VenueId);
    }

    [Fact]
    public void VenueId_returns_the_set_venue()
    {
        var venueId = VenueId.New();
        IVenueContext context = new VenueContext();

        context.Enter(venueId);

        Assert.False(context.IsSystem);
        Assert.Equal(venueId, context.VenueId);
    }
}
