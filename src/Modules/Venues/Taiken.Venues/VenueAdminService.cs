using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Taiken.Shared;
using Taiken.Venues.Contracts;

namespace Taiken.Venues;

/// <summary>
/// Creates and updates venues: the one place ADR-0002/TAI-13 validation (timezone, currency,
/// provider-key format, the plaintext-mode activation guard, credential-schema checks) runs, so
/// nothing can create an inconsistent <see cref="VenueEntity"/> by going around it.
/// </summary>
internal sealed partial class VenueAdminService(VenuesDbContext db, ICredentialProtector protector, TimeProvider timeProvider)
{
    public async Task<Result<VenueId>> CreateAsync(CreateVenueRequest request, CancellationToken cancellationToken = default)
    {
        if (!SlugPattern().IsMatch(request.Slug))
        {
            return Error("venue.invalid_slug", $"Slug must match {SlugPattern()}: '{request.Slug}'.");
        }

        var timezoneResult = ValidateTimezone(request.Timezone);
        if (timezoneResult.IsFailure)
        {
            return Result.Failure<VenueId>(timezoneResult.Error!);
        }

        Currency currency;
        try
        {
            currency = Currency.FromCode(request.CurrencyCode);
        }
        catch (ArgumentException ex)
        {
            return Error("venue.invalid_currency", ex.Message);
        }

        var posProviderResult = ValidateProviderKey(request.PosProvider);
        if (posProviderResult.IsFailure)
        {
            return Result.Failure<VenueId>(posProviderResult.Error!);
        }

        var pspProviderResult = ValidateProviderKey(request.PspProvider);
        if (pspProviderResult.IsFailure)
        {
            return Result.Failure<VenueId>(pspProviderResult.Error!);
        }

        var entity = new VenueEntity
        {
            Id = VenueId.New(),
            Slug = request.Slug,
            Name = request.Name,
            Timezone = request.Timezone,
            Currency = currency,
            Status = VenueStatus.Draft,
            PosProvider = request.PosProvider,
            PspProvider = request.PspProvider,
            PosLocationRef = request.PosLocationRef,
            Config = request.Config ?? new Dictionary<string, string>(),
            ConfigVersion = 0,
            UpdatedAt = timeProvider.GetUtcNow(),
            UpdatedBy = request.UpdatedBy,
        };

        db.Venues.Add(entity);

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            return Error("venue.slug_taken", $"A venue with slug '{request.Slug}' already exists.");
        }

        return Result.Success(entity.Id);
    }

    /// <summary>
    /// ADR-0002/TAI-13 guard: a venue using a real (non-"fake") provider cannot become
    /// <see cref="VenueStatus.Active"/> while credentials are still unencrypted
    /// (<see cref="PlaintextCredentialProtector"/>) — closed by TAI-32, not here.
    /// </summary>
    public async Task<Result> SetStatusAsync(VenueId id, VenueStatus status, string? updatedBy, CancellationToken cancellationToken = default)
    {
        var entity = await db.Venues.SingleOrDefaultAsync(v => v.Id == id, cancellationToken);
        if (entity is null)
        {
            return Result.Failure(new Error("venue.not_found", $"No venue with id '{id.Value}'."));
        }

        if (status == VenueStatus.Active && protector is PlaintextCredentialProtector && !UsesOnlyFakeProviders(entity))
        {
            return Result.Failure(new Error(
                "venue.credentials_not_encrypted",
                "A venue with a non-fake provider cannot become active until TAI-32 (envelope encryption) is closed."));
        }

        entity.Status = status;
        entity.UpdatedAt = timeProvider.GetUtcNow();
        entity.UpdatedBy = updatedBy;
        await db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    /// <summary>
    /// Replaces every secret for (venue, scope). Requires the ambient <c>IVenueContext</c> to
    /// already be scoped to <paramref name="id"/> (ADR 0002 — <see cref="VenueSecretEntity"/> is
    /// venue-scoped data). <paramref name="schema"/> is optional on purpose: TAI-13 has no adapter
    /// yet to declare one (Pos/Psp.Fake land in TAI-16/21), so nothing is checked unless the
    /// caller supplies a schema explicitly.
    /// </summary>
    public async Task<Result> SetSecretsAsync(
        VenueId id,
        CredentialScope scope,
        IReadOnlyDictionary<string, string> values,
        CredentialSchema? schema,
        CancellationToken cancellationToken = default)
    {
        if (schema is not null)
        {
            var missing = schema.RequiredKeys.Where(key => !values.ContainsKey(key)).ToList();
            if (missing.Count > 0)
            {
                return Result.Failure(new Error(
                    "venue.missing_credential_keys",
                    $"Missing required credential keys for scope {scope}: {string.Join(", ", missing)}."));
            }
        }

        var existing = await db.VenueSecrets.Where(s => s.VenueId == id && s.Scope == scope).ToListAsync(cancellationToken);
        db.VenueSecrets.RemoveRange(existing);

        foreach (var (key, value) in values)
        {
            db.VenueSecrets.Add(new VenueSecretEntity
            {
                Id = Guid.CreateVersion7(),
                VenueId = id,
                Scope = scope,
                Key = key,
                ValueEnc = protector.Protect(value),
            });
        }

        await db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    private static bool UsesOnlyFakeProviders(VenueEntity entity) =>
        entity.PosProvider == "fake" && entity.PspProvider == "fake";

    // TAI-13 scope: syntactic only. Validating pos_provider/psp_provider against the set of
    // actually-registered adapters is deferred to TAI-16/21, which are the first adapters to
    // exist at all (see the issue's provider-catalog discussion).
    private static Result ValidateProviderKey(string key) => ProviderKeyPattern().IsMatch(key)
        ? Result.Success()
        : Result.Failure(new Error("venue.invalid_provider_key", $"Provider key must match {ProviderKeyPattern()}: '{key}'."));

    private static Result ValidateTimezone(string timezone)
    {
        try
        {
            TimeZoneInfo.FindSystemTimeZoneById(timezone);
            return Result.Success();
        }
        catch (TimeZoneNotFoundException)
        {
            return Result.Failure(new Error("venue.invalid_timezone", $"Unknown IANA timezone id: '{timezone}'."));
        }
        catch (InvalidTimeZoneException)
        {
            return Result.Failure(new Error("venue.invalid_timezone", $"Invalid timezone data for id: '{timezone}'."));
        }
    }

    private static Result<VenueId> Error(string code, string message) => Result.Failure<VenueId>(new Error(code, message));

    [GeneratedRegex("^[a-z][a-z0-9_]*$")]
    private static partial Regex ProviderKeyPattern();

    [GeneratedRegex("^[a-z0-9]+(-[a-z0-9]+)*$")]
    private static partial Regex SlugPattern();
}
