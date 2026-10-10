using System.Text;

namespace Taiken.Venues;

/// <summary>
/// No encryption at all — a placeholder seam until TAI-32 (envelope encryption). Its mere
/// presence as the registered <see cref="ICredentialProtector"/> is what <c>VenueAdminService</c>
/// checks to block activating a venue with a real (non-"fake") provider.
/// </summary>
internal sealed class PlaintextCredentialProtector : ICredentialProtector
{
    public byte[] Protect(string plaintext) => Encoding.UTF8.GetBytes(plaintext);

    public string Unprotect(byte[] protectedValue) => Encoding.UTF8.GetString(protectedValue);
}
