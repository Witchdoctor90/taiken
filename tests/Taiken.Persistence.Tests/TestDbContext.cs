using Microsoft.EntityFrameworkCore;
using Taiken.Shared;

namespace Taiken.Persistence.Tests;

internal sealed class TestNote
{
    public Guid Id { get; init; }

    // Not `required`: some tests leave it unset on purpose, to prove the interceptor stamps it.
    public VenueId VenueId { get; set; } = null!;

    public string Text { get; set; } = "";
}

/// <summary>Same shape as <see cref="TestNote"/>, but opted out — its <c>VenueId</c> is not "belongs to this venue".</summary>
internal sealed class GlobalNote
{
    public Guid Id { get; init; }

    public required VenueId VenueId { get; set; }

    public string Text { get; set; } = "";
}

internal sealed class TestDbContext(DbContextOptions<TestDbContext> options, IVenueContext venueContext)
    : ModuleDbContext(options, venueContext)
{
    public DbSet<TestNote> Notes => Set<TestNote>();

    public DbSet<GlobalNote> GlobalNotes => Set<GlobalNote>();

    protected override string Schema => "persistence_tests";

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<GlobalNote>().IsNotVenueScoped();

        base.OnModelCreating(modelBuilder);
    }
}
