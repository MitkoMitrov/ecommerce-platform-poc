using Commerce.Api.Domain.Carts;
using Commerce.Api.Domain.Products;
using Commerce.Api.Domain.Purchases;
using Microsoft.EntityFrameworkCore;

namespace Commerce.Api.Infrastructure.Persistence;

public sealed class CommerceDbContext : DbContext
{
    public CommerceDbContext(DbContextOptions<CommerceDbContext> options)
        : base(options)
    {
    }

    public DbSet<Product> Products => Set<Product>();

    public DbSet<Cart> Carts => Set<Cart>();

    public DbSet<CartItem> CartItems => Set<CartItem>();

    public DbSet<Purchase> Purchases => Set<Purchase>();

    public DbSet<PurchaseItem> PurchaseItems => Set<PurchaseItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(CommerceDbContext).Assembly);
    }
}
