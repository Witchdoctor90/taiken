using Testcontainers.PostgreSql;
using Xunit;

namespace Taiken.Testing;

/// <summary>
/// A real Postgres via Testcontainers, shared by every test project that needs one (CLAUDE.md
/// forbids EF InMemory/SQLite for these tests: they don't exercise query filters, transactions,
/// or unique indexes). Starts and disposes the container; schema creation is project-specific
/// (each project's <see cref="DbContext"/> differs), so derive and implement
/// <see cref="CreateSchemaAsync"/>.
/// </summary>
public abstract class PostgreSqlFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:18.6-alpine").Build();

    public string ConnectionString => _container.GetConnectionString();

    public async Task InitializeAsync()
    {
        await _container.StartAsync();
        await CreateSchemaAsync();
    }

    public Task DisposeAsync() => _container.DisposeAsync().AsTask();

    protected abstract Task CreateSchemaAsync();
}
