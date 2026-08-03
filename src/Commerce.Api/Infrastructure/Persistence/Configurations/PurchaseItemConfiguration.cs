using Commerce.Api.Domain.Purchases;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Commerce.Api.Infrastructure.Persistence.Configurations;

public sealed class PurchaseItemConfiguration : IEntityTypeConfiguration<PurchaseItem>
{
    public void Configure(EntityTypeBuilder<PurchaseItem> builder)
    {
        builder.ToTable("purchase_items", table =>
        {
            table.HasCheckConstraint("ck_purchase_items_unit_price_non_negative", "\"unit_price\" >= 0");
            table.HasCheckConstraint("ck_purchase_items_quantity_range", "\"quantity\" >= 1 AND \"quantity\" <= 99");
        });

        builder.HasKey(purchaseItem => new { purchaseItem.PurchaseId, purchaseItem.ProductId })
            .HasName("pk_purchase_items");

        builder.Property(purchaseItem => purchaseItem.PurchaseId)
            .HasColumnName("purchase_id")
            .ValueGeneratedNever();

        builder.Property(purchaseItem => purchaseItem.ProductId)
            .HasColumnName("product_id")
            .ValueGeneratedNever();

        builder.Property(purchaseItem => purchaseItem.ProductName)
            .HasColumnName("product_name")
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(purchaseItem => purchaseItem.UnitPrice)
            .HasColumnName("unit_price")
            .IsRequired()
            .HasColumnType("numeric(18,2)");

        builder.Property(purchaseItem => purchaseItem.Currency)
            .HasColumnName("currency")
            .IsRequired()
            .HasMaxLength(3)
            .IsFixedLength();

        builder.Property(purchaseItem => purchaseItem.Quantity)
            .HasColumnName("quantity")
            .IsRequired();

        builder.Ignore(purchaseItem => purchaseItem.LineTotal);

        builder.HasIndex(purchaseItem => purchaseItem.ProductId)
            .HasDatabaseName("ix_purchase_items_product_id")
            .IsUnique(false);
    }
}
