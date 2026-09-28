using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;

namespace Taiken.Persistence.Tests;

/// <summary>
/// A real Postgres via Testcontainers (CLAUDE.md forbids EF InMemory/SQLite for these tests: they
/// don't exercise query filters, transactions, or unique indexes). TAI-12 will centralize this
/// fixture for every test project; for now each project that needs it owns a small copy.
/// </summary>
public sealed class PostgreSqlFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:18.6-alpine").Build();

    public string ConnectionString => _container.GetConnectionString();

    public async Task InitializeAsync()
    {
        await _container.StartAsync();

        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseNpgsql(ConnectionString)
            .Options;
        await using var context = new TestDbContext(options, new VenueContext());
        await context.Database.EnsureCreatedAsync();
    }

    public Task DisposeAsync() => _container.DisposeAsync().AsTask();
}

[CollectionDefinition(Name)]
public sealed class PostgreSqlCollection : ICollectionFixture<PostgreSqlFixture>
{
    public const string Name = "Postgres";
}
