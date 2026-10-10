using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Npgsql;
using Taiken.Shared;

namespace Taiken.Persistence.Tests;

/// <summary>
/// ADR 0002: every entity with a <c>VenueId</c>-typed <c>VenueId</c> property is either venue-
/// filtered or carries an explicit, visible opt-out — never neither (silently unfiltered) and
/// never both (the opt-out would be a lie).
/// </summary>
public sealed class VenueScopeModelTests
{
    [Fact]
    public void Every_venue_id_entity_is_filtered_or_explicitly_opted_out()
    {
        var model = BuildModel();

        var venueIdEntities = VenueIdEntities(model).ToList();

        // Sanity check on the fixture itself: TestNote (filtered) and GlobalNote (opted out)
        // must both be present, or this test would pass vacuously without checking anything.
        Assert.Contains(venueIdEntities, e => e.ClrType == typeof(TestNote));
        Assert.Contains(venueIdEntities, e => e.ClrType == typeof(GlobalNote));

        foreach (var entityType in venueIdEntities)
        {
            var isFiltered = IsVenueFiltered(entityType);
            var isOptedOut = entityType.IsVenueScopeOptedOut();

            Assert.True(isFiltered ^ isOptedOut,
                $"{entityType.ClrType.Name} must be either venue-filtered or explicitly opted " +
                $"out via .{nameof(VenueScopeModelBuilderExtensions.IsNotVenueScoped)}(), not both or neither.");
        }
    }

    // ADR 0002: the model test above is a pure in-memory check; this one cross-checks it against
    // the real database catalog, so a hand-edited migration that adds a `venue_id` column EF
    // doesn't know about would show up here as a mismatch rather than a silent gap.
    [Collection(PostgreSqlCollection.Name)]
    public sealed class AgainstTheRealDatabase(PersistenceFixture fixture)
    {
        [Fact]
        public async Task Every_venue_id_column_belongs_to_a_filtered_or_opted_out_entity()
        {
            var model = BuildModel();
            var accountedForTables = VenueIdEntities(model)
                .Where(e => IsVenueFiltered(e) || e.IsVenueScopeOptedOut())
                .Select(e => e.GetTableName())
                .ToHashSet();

            var tablesWithVenueIdColumn = await QueryTablesWithVenueIdColumnAsync();

            Assert.Equal(tablesWithVenueIdColumn, accountedForTables);
        }

        private async Task<HashSet<string?>> QueryTablesWithVenueIdColumnAsync()
        {
            await using var connection = new NpgsqlConnection(fixture.ConnectionString);
            await connection.OpenAsync();

            await using var command = connection.CreateCommand();
            command.CommandText =
                "select table_name from information_schema.columns " +
                "where table_schema = 'persistence_tests' and column_name = 'venue_id'";

            var tables = new HashSet<string?>();
            await using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                tables.Add(reader.GetString(0));
            }

            return tables;
        }
    }

    private static IEnumerable<IEntityType> VenueIdEntities(IModel model) =>
        model.GetEntityTypes().Where(e => e.FindProperty(nameof(VenueId)) is { ClrType: var clrType } && clrType == typeof(VenueId));

    private static bool IsVenueFiltered(IEntityType entityType) =>
        entityType.FindDeclaredQueryFilter(ModuleDbContext.VenueIsolationFilterName) is not null;

    private static IModel BuildModel()
    {
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseNpgsql("Host=localhost;Database=model-only")
            .Options;
        using var context = new TestDbContext(options, new VenueContext());
        return context.Model;
    }
}
