namespace Taiken.ArchitectureTests;

/// <summary>
/// Dependency rules from TAI-5 / the README of each <c>src/</c> folder, enforced on the project
/// graph (see <see cref="ProjectGraph"/>) rather than on compiled assemblies — the rules apply
/// just as well before a module or adapter exists as after, with no per-module registration.
/// </summary>
public sealed class DependencyRuleTests
{
    [Fact]
    public void Contracts_and_abstractions_depend_only_on_shared()
    {
        var violations = ProjectGraph.LoadSrcProjects()
            .Where(p => p.Category is ProjectCategory.Contracts or ProjectCategory.Abstractions)
            .SelectMany(p => p.References
                .Where(r => r.Name != "Taiken.Shared")
                .Select(r => $"{p.RelativePath} -> {r.Name}"))
            .ToList();

        Assert.True(violations.Count == 0,
            $"*.Contracts and *.Abstractions may depend only on Taiken.Shared:\n{Describe(violations)}");
    }

    [Fact]
    public void A_module_does_not_reference_another_modules_implementation()
    {
        var violations = ProjectGraph.LoadSrcProjects()
            .Where(p => p.Category == ProjectCategory.ModuleImpl)
            .SelectMany(p => p.References
                .Where(r => r.Category == ProjectCategory.ModuleImpl && r.GroupName != p.GroupName)
                .Select(r => $"{p.RelativePath} -> {r.Name}"))
            .ToList();

        Assert.True(violations.Count == 0,
            $"A module may depend on another module's Contracts only, never its implementation:\n{Describe(violations)}");
    }

    [Fact]
    public void Core_does_not_depend_on_modules_or_integrations()
    {
        var violations = ProjectGraph.LoadSrcProjects()
            .Where(p => p.Category == ProjectCategory.Core)
            .SelectMany(p => p.References
                .Where(r => r.Category is ProjectCategory.ModuleImpl or ProjectCategory.Contracts
                    or ProjectCategory.Adapter or ProjectCategory.Abstractions)
                .Select(r => $"{p.RelativePath} -> {r.Name}"))
            .ToList();

        Assert.True(violations.Count == 0,
            $"Core (src/Core) may not depend on Modules or Integrations:\n{Describe(violations)}");
    }

    [Fact]
    public void An_adapter_depends_only_on_its_own_abstractions_and_shared()
    {
        var violations = ProjectGraph.LoadSrcProjects()
            .Where(p => p.Category == ProjectCategory.Adapter)
            .SelectMany(p => p.References
                .Where(r => r.Name != "Taiken.Shared"
                    && !(r.Category == ProjectCategory.Abstractions && r.GroupName == p.GroupName))
                .Select(r => $"{p.RelativePath} -> {r.Name}"))
            .ToList();

        Assert.True(violations.Count == 0,
            $"An adapter may depend only on its own Abstractions and Taiken.Shared:\n{Describe(violations)}");
    }

    [Fact]
    public void Only_host_references_adapters()
    {
        var violations = ProjectGraph.LoadSrcProjects()
            .Where(p => p.Category != ProjectCategory.Host)
            .SelectMany(p => p.References
                .Where(r => r.Category == ProjectCategory.Adapter)
                .Select(r => $"{p.RelativePath} -> {r.Name}"))
            .ToList();

        Assert.True(violations.Count == 0,
            $"Only Taiken.Api (Host) may reference an adapter directly:\n{Describe(violations)}");
    }

    [Fact]
    public void Nothing_in_src_references_a_fake()
    {
        var violations = ProjectGraph.LoadSrcProjects()
            .Where(p => p.Category != ProjectCategory.Fake)
            .SelectMany(p => p.References
                .Where(r => r.Category == ProjectCategory.Fake)
                .Select(r => $"{p.RelativePath} -> {r.Name}"))
            .ToList();

        Assert.True(violations.Count == 0,
            $"src/Fakes/* is a standalone process; nothing under src/ may reference it:\n{Describe(violations)}");
    }

    [Fact]
    public void A_fake_does_not_reference_anything_in_src()
    {
        var violations = ProjectGraph.LoadSrcProjects()
            .Where(p => p.Category == ProjectCategory.Fake)
            .SelectMany(p => p.References.Select(r => $"{p.RelativePath} -> {r.Name}"))
            .ToList();

        Assert.True(violations.Count == 0,
            $"A fake talks to its adapter only over HTTP; it may not reference anything under src/:\n{Describe(violations)}");
    }

    private static string Describe(IEnumerable<string> violations) => string.Join('\n', violations);
}
