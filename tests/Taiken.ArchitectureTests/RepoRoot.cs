namespace Taiken.ArchitectureTests;

internal static class RepoRoot
{
    /// <summary>Walks up from the test run directory to the repo root (marked by <c>Taiken.slnx</c>).</summary>
    public static string Find()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Taiken.slnx")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName
            ?? throw new InvalidOperationException($"Could not find Taiken.slnx above {AppContext.BaseDirectory}.");
    }
}
