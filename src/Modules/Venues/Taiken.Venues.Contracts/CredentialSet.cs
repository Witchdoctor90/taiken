namespace Taiken.Venues.Contracts;

/// <summary>
/// A venue's decrypted credentials for one <see cref="CredentialScope"/>. Wraps the dictionary
/// instead of handing it out directly so a stray log call (<c>ToString()</c>, structured logging
/// serializers that enumerate dictionaries) can't leak secret values, even under
/// <c>PlaintextCredentialProtector</c> before TAI-32 closes the encryption seam.
/// </summary>
public sealed class CredentialSet(IReadOnlyDictionary<string, string> values)
{
    public static readonly CredentialSet Empty = new(new Dictionary<string, string>());

    public IReadOnlyCollection<string> Keys => values.Keys.ToArray();

    public bool TryGetValue(string key, out string value) => values.TryGetValue(key, out value!);

    public override string ToString() => $"CredentialSet[{values.Count} keys]";
}
