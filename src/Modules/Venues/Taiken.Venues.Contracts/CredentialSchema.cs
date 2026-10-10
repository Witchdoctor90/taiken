namespace Taiken.Venues.Contracts;

/// <summary>
/// Declared by an adapter (<c>Taiken.&lt;Kind&gt;.&lt;Provider&gt;</c>) to name the credential keys
/// it needs. Passed into the venue admin save path to validate <c>venue_secrets</c> at write time.
/// TAI-13 scope note: no adapter exists yet to declare one (Pos/Psp.Fake land in TAI-16/21), so
/// nothing is validated against a registry of providers — a schema is validated only when the
/// caller supplies one explicitly.
/// </summary>
public sealed record CredentialSchema(IReadOnlyCollection<string> RequiredKeys);
