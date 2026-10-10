namespace Taiken.Venues;

/// <summary>
/// Internal-only: there is no HTTP surface for venue creation yet (no staff-admin module to
/// authorize it, ADR 0005) — today's only caller is <see cref="DemoVenueSeeder"/>.
/// </summary>
internal sealed record CreateVenueRequest(
    string Slug,
    string Name,
    string Timezone,
    string CurrencyCode,
    string PosProvider,
    string PspProvider,
    string? PosLocationRef = null,
    IReadOnlyDictionary<string, string>? Config = null,
    string? UpdatedBy = null);
