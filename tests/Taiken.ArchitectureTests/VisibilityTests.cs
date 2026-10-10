using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using NetArchTest.Rules;

namespace Taiken.ArchitectureTests;

/// <summary>
/// ADR 0001 / ADR 0004 / CLAUDE.md "Видимість": inside a module (<c>Taiken.&lt;Name&gt;</c>) or an
/// adapter (<c>Taiken.&lt;Kind&gt;.&lt;Provider&gt;</c>), everything is <c>internal</c> except
/// three kinds of necessarily-public surface — a Wolverine handler (Wolverine's default discovery
/// only finds public types), the module's own <c>AddXModule</c>/<c>MapXEndpoints</c> registration
/// extensions (callable from Host, a different assembly), and generated EF migrations
/// (<c>dotnet ef migrations add</c> always scaffolds its <c>Migration</c>/<c>ModelSnapshot</c>
/// classes <c>public</c> — fighting that template on every future migration isn't worth it). The
/// module boundary is held by this test, not by the access modifier alone.
///
/// Add the assembly here when a module or adapter project is created — <c>typeof(SomeType).Assembly</c>
/// for one representative type is enough.
/// </summary>
public sealed class VisibilityTests
{
    private static readonly Assembly[] ModuleAndAdapterAssemblies = [typeof(Venues.ServiceCollectionExtensions).Assembly];

    [Fact]
    public void Public_types_in_modules_and_adapters_follow_the_internal_by_default_convention()
    {
        var publicTypes = Types.InAssemblies(ModuleAndAdapterAssemblies).That().ArePublic().GetTypes();
        var violations = publicTypes.Where(type => !IsAllowed(type)).Select(type => type.FullName).ToList();

        Assert.True(violations.Count == 0,
            "Public type outside the internal-by-default convention (ADR 0001/0004): " + string.Join(", ", violations));
    }

    private static bool IsAllowed(Type type) =>
        type.Name.EndsWith("Handler", StringComparison.Ordinal)
        || IsRegistrationExtensionsClass(type)
        || type.Namespace?.EndsWith(".Migrations", StringComparison.Ordinal) == true;

    // A C# "static class" (abstract + sealed in IL) exposing at least one AddXModule- or
    // MapXEndpoints-shaped extension method — recognized structurally by its first parameter
    // type, not by a hardcoded class name, so it covers every future module without editing this
    // test again.
    private static bool IsRegistrationExtensionsClass(Type type) =>
        type.IsAbstract && type.IsSealed
        && type.GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Any(method => method.GetParameters() is [{ ParameterType: var first }, ..]
                && (first == typeof(IServiceCollection) || first.Name == "IEndpointRouteBuilder"));
}
