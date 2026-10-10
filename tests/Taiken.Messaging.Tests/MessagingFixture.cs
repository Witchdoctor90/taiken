using Microsoft.EntityFrameworkCore;
using Taiken.Testing;

namespace Taiken.Messaging.Tests;

public sealed class MessagingFixture : PostgreSqlFixture
{
    protected override async Task CreateSchemaAsync()
    {
        var options = new DbContextOptionsBuilder<MessagingTestDbContext>()
            .UseNpgsql(ConnectionString)
            .Options;
        await using var context = new MessagingTestDbContext(options);
        await context.Database.EnsureCreatedAsync();
    }
}

[CollectionDefinition(Name)]
public sealed class PostgreSqlCollection : ICollectionFixture<MessagingFixture>
{
    public const string Name = "Postgres";
}
