using Microsoft.EntityFrameworkCore;
using Taiken.Persistence;
using Taiken.Shared;

namespace Taiken.Messaging.Tests;

public sealed class Note
{
    public Guid Id { get; init; }

    public VenueId VenueId { get; set; } = null!;

    public string Text { get; set; } = "";
}

// Public: it's a parameter on the public Wolverine handler below (RecordNoteHandler), same as any
// module's DbContext would need to be.
public sealed class MessagingTestDbContext(DbContextOptions<MessagingTestDbContext> options) : DbContext(options)
{
    public DbSet<Note> Notes => Set<Note>();

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.Properties<VenueId>().HaveConversion<VenueIdValueConverter>();
    }
}
