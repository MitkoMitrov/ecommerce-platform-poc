using Commerce.Api.Domain.Products;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Commerce.Api.Infrastructure.Persistence.Configurations;

public sealed class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("products", table =>
            table.HasCheckConstraint("ck_products_unit_price_non_negative", "\"unit_price\" >= 0"));

        builder.HasKey(product => product.Id)
            .HasName("pk_products");

        builder.Property(product => product.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(product => product.Name)
            .HasColumnName("name")
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(product => product.UnitPrice)
            .HasColumnName("unit_price")
            .IsRequired()
            .HasColumnType("numeric(18,2)");

        builder.Property(product => product.Currency)
            .HasColumnName("currency")
            .IsRequired()
            .HasMaxLength(3)
            .IsFixedLength();

        builder.Property(product => product.IsActive)
            .HasColumnName("is_active")
            .IsRequired();
    }
}
