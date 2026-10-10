using Microsoft.EntityFrameworkCore;
using Taiken.Persistence;
using Taiken.Shared;
using Taiken.Venues.Contracts;

namespace Taiken.Venues.Tests;

[Collection(PostgreSqlCollection.Name)]
public sealed class VenueAdminServiceTests(VenuesFixture fixture)
{
    [Fact]
    public async Task Creates_a_venue_and_the_directory_finds_it_by_id_and_slug()
    {
        await using var db = CreateDb(new VenueContext());
        var admin = new VenueAdminService(db, new PlaintextCredentialProtector(), TimeProvider.System);
        var directory = new VenueDirectory(db);

        var created = await admin.CreateAsync(ValidRequest("find-me"));
        Assert.True(created.IsSuccess);

        var byId = await directory.FindAsync(created.Value);
        Assert.NotNull(byId);
        Assert.Equal("find-me", byId!.Slug);
        Assert.Equal(VenueStatus.Draft, byId.Status);
        Assert.Equal(Currency.Eur, byId.Currency);

        var bySlug = await directory.FindBySlugAsync("find-me");
        Assert.Equal(created.Value, bySlug!.Id);
    }

    [Fact]
    public async Task Rejects_a_duplicate_slug()
    {
        await using var db = CreateDb(new VenueContext());
        var admin = new VenueAdminService(db, new PlaintextCredentialProtector(), TimeProvider.System);

        await admin.CreateAsync(ValidRequest("dup-slug"));
        var second = await admin.CreateAsync(ValidRequest("dup-slug"));

        Assert.True(second.IsFailure);
        Assert.Equal("venue.slug_taken", second.Error!.Code);
    }

    [Fact]
    public async Task Fake_provider_venue_can_activate_in_plaintext_mode()
    {
        await using var db = CreateDb(new VenueContext());
        var admin = new VenueAdminService(db, new PlaintextCredentialProtector(), TimeProvider.System);
        var directory = new VenueDirectory(db);

        var created = await admin.CreateAsync(ValidRequest("fake-activates", pos: "fake", psp: "fake"));
        var activated = await admin.SetStatusAsync(created.Value, VenueStatus.Active, updatedBy: "test");

        Assert.True(activated.IsSuccess);
        var entry = await directory.FindAsync(created.Value);
        Assert.Equal(VenueStatus.Active, entry!.Status);
    }

    [Fact]
    public async Task Real_provider_venue_cannot_activate_while_credentials_are_plaintext()
    {
        await using var db = CreateDb(new VenueContext());
        var admin = new VenueAdminService(db, new PlaintextCredentialProtector(), TimeProvider.System);

        var created = await admin.CreateAsync(ValidRequest("real-blocked", pos: "stripe", psp: "fake"));
        var activated = await admin.SetStatusAsync(created.Value, VenueStatus.Active, updatedBy: "test");

        Assert.True(activated.IsFailure);
        Assert.Equal("venue.credentials_not_encrypted", activated.Error!.Code);
    }

    [Fact]
    public async Task Secrets_round_trip_through_the_credentials_reader_under_the_owning_venues_context()
    {
        await using var adminDb = CreateDb(new VenueContext());
        var admin = new VenueAdminService(adminDb, new PlaintextCredentialProtector(), TimeProvider.System);
        var created = await admin.CreateAsync(ValidRequest("secrets-venue"));

        var set = await admin.SetSecretsAsync(
            created.Value,
            CredentialScope.Psp,
            new Dictionary<string, string> { ["api_key"] = "sk_test_123" },
            schema: null);
        Assert.True(set.IsSuccess);

        var readerContext = new VenueContext();
        readerContext.Enter(created.Value);
        await using var readerDb = CreateDb(readerContext);
        var credentials = new VenueCredentialsReader(readerDb, new PlaintextCredentialProtector());

        var read = await credentials.GetAsync(CredentialScope.Psp);
        Assert.True(read.TryGetValue("api_key", out var value));
        Assert.Equal("sk_test_123", value);
    }

    [Fact]
    public async Task Missing_required_credential_keys_are_rejected_when_a_schema_is_supplied()
    {
        await using var db = CreateDb(new VenueContext());
        var admin = new VenueAdminService(db, new PlaintextCredentialProtector(), TimeProvider.System);
        var created = await admin.CreateAsync(ValidRequest("schema-venue"));

        var result = await admin.SetSecretsAsync(
            created.Value,
            CredentialScope.Pos,
            new Dictionary<string, string>(),
            schema: new CredentialSchema(["location_id"]));

        Assert.True(result.IsFailure);
        Assert.Equal("venue.missing_credential_keys", result.Error!.Code);
    }

    private VenuesDbContext CreateDb(IVenueContext venueContext) => new(
        new DbContextOptionsBuilder<VenuesDbContext>().UseNpgsql(fixture.ConnectionString).Options,
        venueContext);

    private static CreateVenueRequest ValidRequest(string slug, string pos = "fake", string psp = "fake") => new(
        Slug: slug,
        Name: "Test Venue",
        Timezone: "Europe/Tirane",
        CurrencyCode: "EUR",
        PosProvider: pos,
        PspProvider: psp);
}
