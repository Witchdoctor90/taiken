using Microsoft.EntityFrameworkCore;
using Taiken.Persistence;
using Taiken.Testing;

namespace Taiken.Venues.Tests;

public sealed class VenuesFixture : PostgreSqlFixture
{
    protected override async Task CreateSchemaAsync()
    {
        var options = new DbContextOptionsBuilder<VenuesDbContext>().UseNpgsql(ConnectionString).Options;
        await using var context = new VenuesDbContext(options, new VenueContext());
        await context.Database.EnsureCreatedAsync();
    }
}

[CollectionDefinition(Name)]
public sealed class PostgreSqlCollection : ICollectionFixture<VenuesFixture>
{
    public const string Name = "Postgres";
}
