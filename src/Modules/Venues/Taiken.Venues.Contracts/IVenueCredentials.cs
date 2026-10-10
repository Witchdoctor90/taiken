namespace Taiken.Venues.Contracts;

/// <summary>
/// All of the ambient venue's secrets for one provider scope, in a single call. For Payments/POS
/// adapters only — relies on <c>IVenueContext</c> already being scoped to the venue whose secrets
/// are being read (ADR 0002); there is no venue-id parameter here on purpose, the ambient context
/// is the source of truth for "which venue," same as every other venue-scoped read.
/// </summary>
public interface IVenueCredentials
{
    Task<CredentialSet> GetAsync(CredentialScope scope, CancellationToken cancellationToken = default);
}
