using Microsoft.EntityFrameworkCore;
using Taiken.Venues.Contracts;

namespace Taiken.Venues;

/// <summary>
/// Reads <c>venue_secrets</c> for the ambient venue (the Global Query Filter on
/// <see cref="VenueSecretEntity.VenueId"/> does the scoping — ADR 0002), decrypting each value
/// through <see cref="ICredentialProtector"/> before handing it to the caller.
/// </summary>
internal sealed class VenueCredentialsReader(VenuesDbContext db, ICredentialProtector protector) : IVenueCredentials
{
    public async Task<CredentialSet> GetAsync(CredentialScope scope, CancellationToken cancellationToken = default)
    {
        var rows = await db.VenueSecrets
            .Where(s => s.Scope == scope)
            .ToListAsync(cancellationToken);

        if (rows.Count == 0)
        {
            return CredentialSet.Empty;
        }

        var values = rows.ToDictionary(r => r.Key, r => protector.Unprotect(r.ValueEnc));
        return new CredentialSet(values);
    }
}
