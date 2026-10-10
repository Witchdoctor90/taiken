using Taiken.Shared;

namespace Taiken.Venues.Contracts;

/// <summary>Public, non-secret venue configuration (ADR 0002 / TAI-13). Never carries credentials — see <see cref="IVenueCredentials"/>.</summary>
public sealed record VenueDirectoryEntry(
    VenueId Id,
    string Slug,
    string Name,
    string Timezone,
    Currency Currency,
    VenueStatus Status,
    string PosProvider,
    string PspProvider,
    IReadOnlyDictionary<string, string> Config,
    long ConfigVersion);
