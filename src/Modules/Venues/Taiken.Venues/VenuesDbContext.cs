using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Taiken.Persistence;
using Taiken.Shared;

namespace Taiken.Venues;

internal sealed class VenuesDbContext(DbContextOptions<VenuesDbContext> options, IVenueContext venueContext)
    : ModuleDbContext(options, venueContext)
{
    public DbSet<VenueEntity> Venues => Set<VenueEntity>();

    public DbSet<VenueSecretEntity> VenueSecrets => Set<VenueSecretEntity>();

    protected override string Schema => "venues";

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<VenueEntity>(entity =>
        {
            entity.HasKey(v => v.Id);
            entity.HasIndex(v => v.Slug).IsUnique();
            entity.Property(v => v.Status).HasConversion<string>();
            entity.Property(v => v.Currency).HasConversion(c => c.Code, code => Currency.FromCode(code));

            entity.Property(v => v.Config)
                .HasConversion(
                    value => JsonSerializer.Serialize(value, (JsonSerializerOptions?)null),
                    json => JsonSerializer.Deserialize<Dictionary<string, string>>(json, (JsonSerializerOptions?)null)
                        ?? new Dictionary<string, string>())
                .HasColumnType("jsonb")
                .Metadata.SetValueComparer(ConfigValueComparer);
        });

        modelBuilder.Entity<VenueSecretEntity>(entity =>
        {
            entity.HasKey(s => s.Id);
            entity.Property(s => s.Scope).HasConversion<string>();
            entity.HasIndex(s => new { s.VenueId, s.Scope, s.Key }).IsUnique();
            entity.HasOne<VenueEntity>().WithMany().HasForeignKey(s => s.VenueId);
        });

        base.OnModelCreating(modelBuilder);
    }

    // EF warns without one for a mutable reference-typed property (jsonb-mapped dictionary);
    // structural equality so SaveChanges detects an actually-changed Config, not just a new instance.
    private static readonly ValueComparer<IReadOnlyDictionary<string, string>> ConfigValueComparer = new(
        (left, right) => ConfigEquals(left, right),
        value => value.Aggregate(17, (hash, pair) => HashCode.Combine(hash, pair.Key, pair.Value)),
        value => new Dictionary<string, string>(value));

    private static bool ConfigEquals(IReadOnlyDictionary<string, string>? left, IReadOnlyDictionary<string, string>? right)
    {
        if (ReferenceEquals(left, right))
        {
            return true;
        }

        if (left is null || right is null || left.Count != right.Count)
        {
            return false;
        }

        foreach (var (key, value) in left)
        {
            if (!right.TryGetValue(key, out var otherValue) || otherValue != value)
            {
                return false;
            }
        }

        return true;
    }
}
