using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Taiken.Venues.Contracts;

namespace Taiken.Venues;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddVenuesModule(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Postgres")
            ?? throw new InvalidOperationException(
                "Missing connection string 'Postgres'. Set it via `dotnet user-secrets set ConnectionStrings:Postgres \"...\"`.");

        services.AddDbContext<VenuesDbContext>(options => options.UseNpgsql(connectionString));

        services.AddScoped<IVenueDirectory, VenueDirectory>();
        services.AddScoped<IVenueCredentials, VenueCredentialsReader>();
        services.AddScoped<VenueAdminService>();

        // PlaintextCredentialProtector until TAI-32 (envelope encryption) replaces it — swapping
        // it is the only change TAI-32 needs here; VenueAdminService's activation guard checks
        // for this exact type to know encryption isn't closed yet.
        services.AddSingleton<ICredentialProtector, PlaintextCredentialProtector>();

        services.AddHostedService<DemoVenueSeeder>();

        return services;
    }
}
