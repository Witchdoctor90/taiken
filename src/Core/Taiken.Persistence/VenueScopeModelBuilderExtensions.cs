using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Taiken.Persistence;

public static class VenueScopeModelBuilderExtensions
{
    internal const string NotVenueScopedAnnotation = "Taiken:NotVenueScoped";

    /// <summary>
    /// Explicit opt-out from the automatic <c>VenueId</c> isolation filter (ADR 0002). Use only
    /// when the entity's <c>VenueId</c> property does not mean "belongs to this venue".
    /// </summary>
    public static EntityTypeBuilder<TEntity> IsNotVenueScoped<TEntity>(this EntityTypeBuilder<TEntity> builder)
        where TEntity : class
    {
        builder.Metadata.SetAnnotation(NotVenueScopedAnnotation, true);
        return builder;
    }
}
