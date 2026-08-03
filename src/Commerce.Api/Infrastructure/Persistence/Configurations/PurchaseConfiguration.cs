using Commerce.Api.Domain.Purchases;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Commerce.Api.Infrastructure.Persistence.Configurations;

public sealed class PurchaseConfiguration : IEntityTypeConfiguration<Purchase>
{
    public void Configure(EntityTypeBuilder<Purchase> builder)
    {
        builder.ToTable("purchases", table =>
            table.HasCheckConstraint("ck_purchases_total_non_negative", "\"total\" >= 0"));

        builder.HasKey(purchase => purchase.Id)
            .HasName("pk_purchases");

        builder.Property(purchase => purchase.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        // Historical reference only — intentionally not a foreign key, so a Purchase survives
        // regardless of what later happens to the Cart it was created from.
        builder.Property(purchase => purchase.CartId)
            .HasColumnName("cart_id")
            .IsRequired();

        builder.Property(purchase => purchase.PurchasedAtUtc)
            .HasColumnName("purchased_at_utc")
            .IsRequired()
            .HasColumnType("timestamp with time zone");

        builder.Property(purchase => purchase.Currency)
            .HasColumnName("currency")
            .IsRequired()
            .HasMaxLength(3)
            .IsFixedLength();

        builder.Property(purchase => purchase.Total)
            .HasColumnName("total")
            .IsRequired()
            .HasColumnType("numeric(18,2)");

        builder.HasMany(purchase => purchase.Items)
            .WithOne()
            .HasForeignKey(purchaseItem => purchaseItem.PurchaseId)
            .HasConstraintName("fk_purchase_items_purchases_purchase_id")
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(purchase => purchase.Items)
            .HasField("_items")
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasIndex(purchase => new { purchase.PurchasedAtUtc, purchase.Id })
            .HasDatabaseName("ix_purchases_purchased_at_utc_id");
    }
}
