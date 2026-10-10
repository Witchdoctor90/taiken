using Microsoft.EntityFrameworkCore;

namespace Taiken.FakePos;

internal sealed class FakePosDbContext(DbContextOptions<FakePosDbContext> options) : DbContext(options)
{
    public DbSet<CheckEntity> Checks => Set<CheckEntity>();

    public DbSet<CheckItemEntity> CheckItems => Set<CheckItemEntity>();

    public DbSet<CheckItemModifierEntity> CheckItemModifiers => Set<CheckItemModifierEntity>();

    public DbSet<CheckDiscountEntity> CheckDiscounts => Set<CheckDiscountEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<CheckEntity>(entity =>
        {
            entity.HasKey(c => c.Id);
            entity.Property(c => c.State).HasConversion<string>();
            entity.HasIndex(c => new { c.LocationCode, c.TableNo, c.State });
            entity.HasMany(c => c.Items).WithOne().HasForeignKey(i => i.CheckId);
            entity.HasMany(c => c.Discounts).WithOne().HasForeignKey(d => d.CheckId);
        });

        modelBuilder.Entity<CheckItemEntity>(entity =>
        {
            entity.HasKey(i => i.Id);
            entity.HasMany(i => i.Modifiers).WithOne().HasForeignKey(m => m.CheckItemId);
        });

        modelBuilder.Entity<CheckItemModifierEntity>().HasKey(m => m.Id);
        modelBuilder.Entity<CheckDiscountEntity>().HasKey(d => d.Id);

        base.OnModelCreating(modelBuilder);
    }
}
