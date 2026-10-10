using Microsoft.EntityFrameworkCore;
using Taiken.Testing;

namespace Taiken.Persistence.Tests;

public sealed class PersistenceFixture : PostgreSqlFixture
{
    protected override async Task CreateSchemaAsync()
    {
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseNpgsql(ConnectionString)
            .Options;
        await using var context = new TestDbContext(options, new VenueContext());
        await context.Database.EnsureCreatedAsync();
    }
}

[CollectionDefinition(Name)]
public sealed class PostgreSqlCollection : ICollectionFixture<PersistenceFixture>
{
    public const string Name = "Postgres";
}
