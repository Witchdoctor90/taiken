using Microsoft.EntityFrameworkCore;
using Taiken.Persistence;
using Taiken.Shared;

namespace Taiken.Venues.Tests;

/// <summary>
/// Every case here is rejected before <see cref="VenueAdminService"/> ever touches the database,
/// so a connection that's never opened is enough — no Testcontainers, no Docker.
/// </summary>
public sealed class VenueAdminServiceValidationTests
{
    [Theory]
    [InlineData("Not-A-Slug")]
    [InlineData("UPPER")]
    [InlineData("")]
    [InlineData("has space")]
    [InlineData("-leading-dash")]
    public async Task Rejects_an_invalid_slug(string slug)
    {
        var result = await CreateService().CreateAsync(ValidRequest() with { Slug = slug });
        AssertFailure(result, "venue.invalid_slug");
    }

    [Fact]
    public async Task Rejects_an_unknown_timezone()
    {
        var result = await CreateService().CreateAsync(ValidRequest() with { Timezone = "Not/A/Zone" });
        AssertFailure(result, "venue.invalid_timezone");
    }

    [Fact]
    public async Task Rejects_an_unknown_currency_code()
    {
        var result = await CreateService().CreateAsync(ValidRequest() with { CurrencyCode = "XXX" });
        AssertFailure(result, "venue.invalid_currency");
    }

    [Theory]
    [InlineData("Fake")]
    [InlineData("fake-pos")]
    [InlineData("1fake")]
    [InlineData("")]
    public async Task Rejects_an_invalid_provider_key(string provider)
    {
        var result = await CreateService().CreateAsync(ValidRequest() with { PosProvider = provider });
        AssertFailure(result, "venue.invalid_provider_key");
    }

    private static void AssertFailure(Result<VenueId> result, string expectedCode)
    {
        Assert.True(result.IsFailure);
        Assert.Equal(expectedCode, result.Error!.Code);
    }

    private static CreateVenueRequest ValidRequest() => new(
        Slug: "valid-slug",
        Name: "Valid",
        Timezone: "Europe/Tirane",
        CurrencyCode: "ALL",
        PosProvider: "fake",
        PspProvider: "fake");

    private static VenueAdminService CreateService()
    {
        var options = new DbContextOptionsBuilder<VenuesDbContext>()
            .UseNpgsql("Host=localhost;Database=never-opened")
            .Options;
        var db = new VenuesDbContext(options, new VenueContext());
        return new VenueAdminService(db, new PlaintextCredentialProtector(), TimeProvider.System);
    }
}
