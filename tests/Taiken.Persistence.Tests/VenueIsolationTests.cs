using Microsoft.EntityFrameworkCore;
using Taiken.Shared;

namespace Taiken.Persistence.Tests;

[Collection(PostgreSqlCollection.Name)]
public sealed class VenueIsolationTests(PostgreSqlFixture fixture)
{
    [Fact]
    public async Task Venue_a_does_not_see_venue_b_data()
    {
        var venueA = VenueId.New();
        var venueB = VenueId.New();

        await SeedNoteAsync(venueA, "a");
        await SeedNoteAsync(venueB, "b");

        await using var asA = CreateContext(VenueOf(venueA));
        var visibleToA = await asA.Notes.ToListAsync();

        Assert.Single(visibleToA);
        Assert.Equal("a", visibleToA[0].Text);
    }

    [Fact]
    public async Task Venue_a_cannot_find_venue_b_data_by_id()
    {
        var venueB = VenueId.New();
        var noteId = Guid.NewGuid();
        await SeedNoteAsync(venueB, "b", noteId);

        await using var asA = CreateContext(VenueOf(VenueId.New()));
        var found = await asA.Notes.FindAsync(noteId);

        Assert.Null(found);
    }

    [Fact]
    public async Task System_mode_sees_every_venue()
    {
        var venueA = VenueId.New();
        var venueB = VenueId.New();
        await SeedNoteAsync(venueA, "a");
        await SeedNoteAsync(venueB, "b");

        var system = new VenueContext();
        system.EnterSystem();
        await using var asSystem = CreateContext(system);

        var all = await asSystem.Notes.Where(n => n.VenueId == venueA || n.VenueId == venueB).ToListAsync();

        Assert.Equal(2, all.Count);
    }

    [Fact]
    public async Task Query_without_a_scope_throws()
    {
        await using var db = CreateContext(new VenueContext());

        await Assert.ThrowsAsync<InvalidOperationException>(() => db.Notes.ToListAsync());
    }

    [Fact]
    public async Task Opted_out_entity_is_not_filtered()
    {
        var venueA = VenueId.New();
        var venueB = VenueId.New();

        await using (var db = CreateContext(VenueOf(venueA)))
        {
            db.GlobalNotes.Add(new GlobalNote { Id = Guid.NewGuid(), VenueId = venueB, Text = "not really scoped" });
            await db.SaveChangesAsync();
        }

        await using var asA = CreateContext(VenueOf(venueA));
        var visible = await asA.GlobalNotes.Where(n => n.Text == "not really scoped").ToListAsync();

        Assert.Single(visible);
    }

    [Fact]
    public async Task Interceptor_stamps_venue_id_on_new_entities()
    {
        var venueA = VenueId.New();

        await using var db = CreateContext(VenueOf(venueA));
        var note = new TestNote { Id = Guid.NewGuid(), Text = "stamped" };
        db.Add(note);
        await db.SaveChangesAsync();

        Assert.Equal(venueA, note.VenueId);
    }

    [Fact]
    public async Task Interceptor_refuses_to_save_into_a_different_venue()
    {
        var venueA = VenueId.New();
        var venueB = VenueId.New();

        await using var db = CreateContext(VenueOf(venueA));
        db.Add(new TestNote { Id = Guid.NewGuid(), VenueId = venueB, Text = "wrong venue" });

        await Assert.ThrowsAsync<InvalidOperationException>(() => db.SaveChangesAsync());
    }

    private async Task SeedNoteAsync(VenueId venueId, string text, Guid? id = null)
    {
        await using var db = CreateContext(VenueOf(venueId));
        db.Notes.Add(new TestNote { Id = id ?? Guid.NewGuid(), VenueId = venueId, Text = text });
        await db.SaveChangesAsync();
    }

    private static VenueContext VenueOf(VenueId venueId)
    {
        var context = new VenueContext();
        context.Enter(venueId);
        return context;
    }

    private TestDbContext CreateContext(IVenueContext venueContext)
    {
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseNpgsql(fixture.ConnectionString)
            .AddInterceptors(new VenueIsolationSaveChangesInterceptor(venueContext))
            .Options;

        return new TestDbContext(options, venueContext);
    }
}
