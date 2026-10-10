namespace Taiken.ArchitectureTests;

/// <summary>
/// ADR 0002: <c>IgnoreQueryFilters()</c> bypasses venue isolation, so it is allowed only at the
/// explicitly listed call sites below — table-QR resolving, the webhook payment-attempt lookup,
/// and the reconciler. The list lives here, not as a suppressible warning, so a new call site
/// fails CI until someone deliberately adds it to <see cref="Allowlist"/>.
/// </summary>
public sealed class IgnoreQueryFiltersAllowlistTests
{
    // Relative to the repo root, forward slashes. Empty for now: none of the three ADR 0002 call
    // sites exist yet (table-QR resolving, webhook lookup, reconciler all come after M2).
    private static readonly string[] Allowlist = [];

    [Fact]
    public void IgnoreQueryFilters_is_used_only_at_allowlisted_locations()
    {
        var repoRoot = RepoRoot.Find();
        var srcDir = Path.Combine(repoRoot, "src");

        var offenders = Directory.EnumerateFiles(srcDir, "*.cs", SearchOption.AllDirectories)
            .Where(f => File.ReadAllText(f).Contains("IgnoreQueryFilters(", StringComparison.Ordinal))
            .Select(f => Path.GetRelativePath(repoRoot, f).Replace('\\', '/'))
            .Where(relativePath => !Allowlist.Contains(relativePath))
            .ToList();

        Assert.True(offenders.Count == 0,
            "IgnoreQueryFilters() used outside the ADR 0002 allowlist: " + string.Join(", ", offenders) +
            $". Add the file to {nameof(IgnoreQueryFiltersAllowlistTests)}.{nameof(Allowlist)} only if ADR 0002 permits it there.");
    }
}
