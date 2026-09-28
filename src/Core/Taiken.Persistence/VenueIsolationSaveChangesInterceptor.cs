using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Taiken.Shared;

namespace Taiken.Persistence;

/// <summary>
/// Stamps <c>VenueId</c> onto new venue-scoped entities from the current <see cref="IVenueContext"/>,
/// and refuses to save an entity whose <c>VenueId</c> does not match that context (ADR 0002).
/// </summary>
public sealed class VenueIsolationSaveChangesInterceptor(IVenueContext venueContext) : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        Enforce(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        Enforce(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void Enforce(DbContext? context)
    {
        if (context is null)
        {
            return;
        }

        foreach (var entry in context.ChangeTracker.Entries())
        {
            var venueIdProperty = entry.Metadata.FindProperty(nameof(VenueId));
            if (venueIdProperty is null || venueIdProperty.ClrType != typeof(VenueId))
            {
                continue;
            }

            if (entry.Metadata.FindAnnotation(VenueScopeModelBuilderExtensions.NotVenueScopedAnnotation) is not null)
            {
                continue;
            }

            var property = entry.Property(venueIdProperty.Name);

            if (entry.State == EntityState.Added && property.CurrentValue is null)
            {
                if (!venueContext.IsSystem)
                {
                    property.CurrentValue = venueContext.VenueId;
                }

                continue;
            }

            if (entry.State is not (EntityState.Added or EntityState.Modified or EntityState.Deleted))
            {
                continue;
            }

            if (venueContext.IsSystem || property.CurrentValue is not VenueId currentVenueId)
            {
                continue;
            }

            if (currentVenueId != venueContext.VenueId)
            {
                throw new InvalidOperationException(
                    $"Refusing to save {entry.Metadata.ClrType.Name} for venue {currentVenueId} " +
                    $"while the venue context is {venueContext.VenueId}.");
            }
        }
    }
}
