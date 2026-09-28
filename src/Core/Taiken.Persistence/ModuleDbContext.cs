using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Taiken.Shared;

namespace Taiken.Persistence;

/// <summary>
/// Base <see cref="DbContext"/> for a module: one Postgres schema (snake_case columns and table
/// names via EFCore.NamingConventions), and an automatic, named Global Query Filter on every
/// entity that has a <c>VenueId</c> property of type <see cref="VenueId"/> (ADR 0002). Opt out per
/// entity with <see cref="VenueScopeModelBuilderExtensions.IsNotVenueScoped{TEntity}"/>.
/// A derived <c>OnModelCreating</c> must apply that opt-out, then call
/// <c>base.OnModelCreating(modelBuilder)</c> last — this class decides which entities get the
/// filter when it runs, so the opt-out has to be on the model already.
/// </summary>
public abstract class ModuleDbContext : DbContext
{
    private const string VenueIsolationFilterName = "venue";

    protected ModuleDbContext(DbContextOptions options, IVenueContext venueContext)
        : base(options)
    {
        VenueContext = venueContext;
    }

    protected IVenueContext VenueContext { get; }

    protected abstract string Schema { get; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder) =>
        optionsBuilder.UseSnakeCaseNamingConvention();

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.Properties<VenueId>().HaveConversion<VenueIdValueConverter>();
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            var venueIdProperty = entityType.FindProperty(nameof(VenueId));
            if (venueIdProperty is null || venueIdProperty.ClrType != typeof(VenueId))
            {
                continue;
            }

            if (entityType.FindAnnotation(VenueScopeModelBuilderExtensions.NotVenueScopedAnnotation) is not null)
            {
                continue;
            }

            modelBuilder.Entity(entityType.ClrType)
                .HasQueryFilter(VenueIsolationFilterName, BuildVenueIsolationFilter(entityType.ClrType));
        }

        base.OnModelCreating(modelBuilder);
    }

    private LambdaExpression BuildVenueIsolationFilter(Type entityType)
    {
        var entity = Expression.Parameter(entityType, "entity");
        var entityVenueId = Expression.Property(entity, nameof(VenueId));

        // Closes over `this` (the DbContext), not over VenueContext directly, so EF Core rebinds
        // the filter to whichever context instance is actually running the query instead of
        // baking in the instance that happened to build the (cached) model.
        var context = Expression.Constant(this, GetType());
        var venueContext = Expression.Property(context, nameof(VenueContext));
        var isSystem = Expression.Property(venueContext, nameof(IVenueContext.IsSystem));
        var contextVenueId = Expression.Property(venueContext, nameof(IVenueContext.VenueIdOrNull));

        var venueMatches = Expression.Equal(entityVenueId, contextVenueId);
        var body = Expression.OrElse(isSystem, venueMatches);

        return Expression.Lambda(body, entity);
    }
}
