using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;

namespace Taiken.Messaging.Tests;

/// <summary>
/// A real Postgres via Testcontainers (CLAUDE.md forbids EF InMemory/SQLite for these tests).
/// TAI-12 will centralize this fixture for every test project; for now each project that needs
/// it owns a small copy — same as <c>Taiken.Persistence.Tests</c>.
/// </summary>
public sealed class PostgreSqlFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:18.6-alpine").Build();

    public string ConnectionString => _container.GetConnectionString();

    public async Task InitializeAsync()
    {
        await _container.StartAsync();

        var options = new DbContextOptionsBuilder<MessagingTestDbContext>()
            .UseNpgsql(ConnectionString)
            .Options;
        await using var context = new MessagingTestDbContext(options);
        await context.Database.EnsureCreatedAsync();
    }

    public Task DisposeAsync() => _container.DisposeAsync().AsTask();
}

[CollectionDefinition(Name)]
public sealed class PostgreSqlCollection : ICollectionFixture<PostgreSqlFixture>
{
    public const string Name = "Postgres";
}
