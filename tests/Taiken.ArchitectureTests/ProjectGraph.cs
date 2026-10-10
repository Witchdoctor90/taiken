using System.Xml.Linq;

namespace Taiken.ArchitectureTests;

internal enum ProjectCategory
{
    Core,
    Host,
    Contracts,
    ModuleImpl,
    Abstractions,
    Adapter,
    Fake,
    Other,
}

/// <summary>A project under <c>src/</c> and its resolved <c>ProjectReference</c> edges, categorized by folder convention (README.md of each src/ folder).</summary>
internal sealed record ProjectNode(string Name, string RelativePath, ProjectCategory Category, string? GroupName)
{
    public List<ProjectNode> References { get; } = [];
}

/// <summary>
/// Builds the dependency graph of every project under <c>src/</c> by reading <c>ProjectReference</c>
/// elements straight from each <c>.csproj</c> on disk — not from loaded assemblies. This lets the
/// dependency-rule tests (<see cref="DependencyRuleTests"/>) cover modules, adapters, and fakes
/// that don't exist yet without any manual registration: a future module's PR just adds a folder
/// and its project reference, and these tests see it automatically on the next run.
/// </summary>
internal static class ProjectGraph
{
    public static IReadOnlyCollection<ProjectNode> LoadSrcProjects()
    {
        var repoRoot = RepoRoot.Find();
        var srcDir = Path.Combine(repoRoot, "src");
        var csprojPaths = Directory.EnumerateFiles(srcDir, "*.csproj", SearchOption.AllDirectories).ToList();

        var nodesByPath = new Dictionary<string, ProjectNode>(StringComparer.OrdinalIgnoreCase);
        foreach (var path in csprojPaths)
        {
            var relativePath = Path.GetRelativePath(repoRoot, path).Replace('\\', '/');
            var (category, groupName) = Categorize(relativePath);
            nodesByPath[path] = new ProjectNode(Path.GetFileNameWithoutExtension(path), relativePath, category, groupName);
        }

        foreach (var path in csprojPaths)
        {
            var node = nodesByPath[path];
            var projectDirectory = Path.GetDirectoryName(path)!;
            var references = XDocument.Load(path)
                .Descendants("ProjectReference")
                .Select(e => e.Attribute("Include")?.Value)
                .Where(include => !string.IsNullOrWhiteSpace(include));

            foreach (var include in references)
            {
                var resolved = Path.GetFullPath(Path.Combine(projectDirectory, include!));
                if (nodesByPath.TryGetValue(resolved, out var target))
                {
                    node.References.Add(target);
                }
            }
        }

        return nodesByPath.Values;
    }

    private static (ProjectCategory Category, string? GroupName) Categorize(string relativePath)
    {
        var segments = relativePath.Split('/');
        if (segments.Length < 3 || segments[0] != "src")
        {
            return (ProjectCategory.Other, null);
        }

        return segments[1] switch
        {
            "Core" => (ProjectCategory.Core, null),
            "Host" => (ProjectCategory.Host, null),
            "Fakes" => (ProjectCategory.Fake, null),
            "Modules" when segments.Length >= 4 => (
                segments[3].EndsWith(".Contracts", StringComparison.Ordinal) ? ProjectCategory.Contracts : ProjectCategory.ModuleImpl,
                segments[2]),
            "Integrations" when segments.Length >= 4 => (
                segments[3].EndsWith(".Abstractions", StringComparison.Ordinal) ? ProjectCategory.Abstractions : ProjectCategory.Adapter,
                segments[2]),
            _ => (ProjectCategory.Other, null),
        };
    }
}
