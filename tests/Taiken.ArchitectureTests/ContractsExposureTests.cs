using System.Reflection;
using NetArchTest.Rules;

namespace Taiken.ArchitectureTests;

/// <summary>
/// TAI-13 acceptance criterion: <c>*.Contracts</c> never exposes an EF entity type.
/// <see cref="DependencyRuleTests"/> (TAI-12) already proves a Contracts project has no
/// <c>ProjectReference</c> to anything but Shared — but that leaves a gap: nothing stops someone
/// from adding a NuGet <c>PackageReference</c> directly to the Contracts csproj (e.g. EF Core)
/// and defining or re-exposing an entity-shaped type right there, since a project reference graph
/// can't see package references. This test closes that gap by checking the compiled IL for any
/// dependency on EF Core at all, not just the project file.
///
/// Add the Contracts assembly here when a module's Contracts project is created.
/// </summary>
public sealed class ContractsExposureTests
{
    private static readonly Assembly[] ContractsAssemblies = [typeof(Venues.Contracts.IVenueDirectory).Assembly];

    [Fact]
    public void Contracts_does_not_depend_on_ef_core()
    {
        var result = Types.InAssemblies(ContractsAssemblies)
            .ShouldNot().HaveDependencyOnAny("Microsoft.EntityFrameworkCore")
            .GetResult();

        Assert.True(result.IsSuccessful,
            "A *.Contracts type depends on Microsoft.EntityFrameworkCore — likely an EF entity type " +
            "leaking through a public method, or EF referenced directly where only Shared is allowed " +
            "(ADR 0001): " + string.Join(", ", (result.FailingTypes ?? []).Select(t => t.FullName)));
    }
}
