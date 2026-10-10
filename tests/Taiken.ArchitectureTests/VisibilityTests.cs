using System.Reflection;
using NetArchTest.Rules;

namespace Taiken.ArchitectureTests;

/// <summary>
/// ADR 0004 / CLAUDE.md "Видимість": inside a module (<c>Taiken.&lt;Name&gt;</c>) or an adapter
/// (<c>Taiken.&lt;Kind&gt;.&lt;Provider&gt;</c>), everything is <c>internal</c> except Wolverine
/// handlers, which must be <c>public</c> for Wolverine's default discovery. The module boundary is
/// held by this test, not by the access modifier alone.
///
/// Add the assembly here when a module or adapter project is created — <c>typeof(SomeType).Assembly</c>
/// for one representative type is enough. There is nothing to add yet: no module or adapter exists
/// before TAI-13.
/// </summary>
public sealed class VisibilityTests
{
    private static readonly Assembly[] ModuleAndAdapterAssemblies = [];

    [Fact]
    public void Public_types_in_modules_and_adapters_are_wolverine_handlers()
    {
        var result = Types.InAssemblies(ModuleAndAdapterAssemblies)
            .That().ArePublic()
            .Should().HaveNameEndingWith("Handler")
            .GetResult();

        Assert.True(result.IsSuccessful,
            "Public type outside the internal/Handler convention: " +
            string.Join(", ", (result.FailingTypes ?? []).Select(t => t.FullName)));
    }
}
