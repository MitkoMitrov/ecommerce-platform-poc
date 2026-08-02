using Commerce.Api.Domain.Carts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Commerce.Api.Infrastructure.Persistence.Configurations;

public sealed class CartConfiguration : IEntityTypeConfiguration<Cart>
{
    public void Configure(EntityTypeBuilder<Cart> builder)
    {
        builder.ToTable("carts");

        builder.HasKey(cart => cart.Id)
            .HasName("pk_carts");

        builder.Property(cart => cart.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(cart => cart.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .IsRequired()
            .HasColumnType("timestamp with time zone");

        builder.Property(cart => cart.UpdatedAtUtc)
            .HasColumnName("updated_at_utc")
            .IsRequired()
            .HasColumnType("timestamp with time zone");

        builder.Ignore(cart => cart.Subtotal);
        builder.Ignore(cart => cart.Currency);

        builder.HasMany(cart => cart.Items)
            .WithOne()
            .HasForeignKey(cartItem => cartItem.CartId)
            .HasConstraintName("fk_cart_items_carts_cart_id")
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(cart => cart.Items)
            .HasField("_items")
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
