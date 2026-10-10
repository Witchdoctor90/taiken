namespace Taiken.Venues;

/// <summary>
/// The encryption seam for <c>venue_secrets.value_enc</c> (TAI-13). All of Venues goes through
/// this interface, never the raw bytes directly, so swapping <see cref="PlaintextCredentialProtector"/>
/// for envelope encryption (TAI-32) touches only its registration in <c>AddVenuesModule</c>.
/// </summary>
internal interface ICredentialProtector
{
    byte[] Protect(string plaintext);

    string Unprotect(byte[] protectedValue);
}
