using Commerce.Api.Domain.Carts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Commerce.Api.Infrastructure.Persistence.Configurations;

public sealed class CartItemConfiguration : IEntityTypeConfiguration<CartItem>
{
    public void Configure(EntityTypeBuilder<CartItem> builder)
    {
        builder.ToTable("cart_items", table =>
        {
            table.HasCheckConstraint("ck_cart_items_unit_price_non_negative", "\"unit_price_snapshot\" >= 0");
            table.HasCheckConstraint("ck_cart_items_quantity_range", "\"quantity\" >= 1 AND \"quantity\" <= 99");
        });

        builder.HasKey(cartItem => new { cartItem.CartId, cartItem.ProductId })
            .HasName("pk_cart_items");

        builder.Property(cartItem => cartItem.CartId)
            .HasColumnName("cart_id")
            .ValueGeneratedNever();

        builder.Property(cartItem => cartItem.ProductId)
            .HasColumnName("product_id")
            .ValueGeneratedNever();

        builder.Property(cartItem => cartItem.ProductNameSnapshot)
            .HasColumnName("product_name_snapshot")
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(cartItem => cartItem.UnitPriceSnapshot)
            .HasColumnName("unit_price_snapshot")
            .IsRequired()
            .HasColumnType("numeric(18,2)");

        builder.Property(cartItem => cartItem.Currency)
            .HasColumnName("currency")
            .IsRequired()
            .HasMaxLength(3)
            .IsFixedLength();

        builder.Property(cartItem => cartItem.Quantity)
            .HasColumnName("quantity")
            .IsRequired();

        builder.Ignore(cartItem => cartItem.LineTotal);

        builder.HasIndex(cartItem => cartItem.ProductId)
            .HasDatabaseName("ix_cart_items_product_id")
            .IsUnique(false);
    }
}
