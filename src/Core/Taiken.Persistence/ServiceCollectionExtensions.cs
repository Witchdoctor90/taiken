using Microsoft.Extensions.DependencyInjection;

namespace Taiken.Persistence;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddVenueContext(this IServiceCollection services) =>
        services.AddScoped<IVenueContext, VenueContext>();
}
