using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Taiken.Shared;
using Taiken.Venues.Contracts;

namespace Taiken.Venues;

/// <summary>
/// Demo venue, up in one step on a clean DB (TAI-13 acceptance criteria) — Development/Staging
/// only, never production. Runtime seeding, not a migration: a migration would have to re-run
/// identically forever; a hosted service can just check "does it already exist."
/// </summary>
internal sealed class DemoVenueSeeder(
    IServiceScopeFactory scopeFactory,
    IHostEnvironment environment,
    ILogger<DemoVenueSeeder> logger) : IHostedService
{
    // Fixed, not Guid.CreateVersion7(): the seed must be idempotent across restarts, which a
    // time-based id can't give you — the same demo venue has to come back as the same row.
    private static readonly VenueId DemoVenueId = new(Guid.Parse("00000000-0000-7000-8000-000000000001"));

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (!environment.IsDevelopment() && !environment.IsStaging())
        {
            return;
        }

        using var scope = scopeFactory.CreateScope();
        var directory = scope.ServiceProvider.GetRequiredService<IVenueDirectory>();
        if (await directory.FindAsync(DemoVenueId, cancellationToken) is not null)
        {
            return;
        }

        var admin = scope.ServiceProvider.GetRequiredService<VenueAdminService>();
        var created = await admin.CreateAsync(
            new CreateVenueRequest(
                Slug: "demo",
                Name: "Demo Venue",
                Timezone: "Europe/Tirane",
                CurrencyCode: "ALL",
                PosProvider: "fake",
                PspProvider: "fake",
                UpdatedBy: "seed"),
            cancellationToken);

        if (created.IsFailure)
        {
            logger.LogError("Failed to seed the demo venue: {Code} {Message}", created.Error!.Code, created.Error.Message);
            return;
        }

        // Both providers are "fake", so the plaintext-mode guard (TAI-13/VenueAdminService) never
        // blocks this activation — it's there specifically to allow this.
        await admin.SetStatusAsync(created.Value, VenueStatus.Active, updatedBy: "seed", cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
