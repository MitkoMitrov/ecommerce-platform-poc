using Commerce.Api.Domain.Products;

namespace Commerce.UnitTests.Domain.Products;

public class ProductTests
{
    [Fact]
    public void Create_WithValidValues_CreatesNormalizedActiveProduct()
    {
        var id = Guid.Parse("11111111-1111-1111-1111-111111111111");

        var product = Product.Create(id, "  Wireless Mouse  ", 29.99m, "eur", true);

        Assert.Equal(id, product.Id);
        Assert.Equal("Wireless Mouse", product.Name);
        Assert.Equal(29.99m, product.UnitPrice);
        Assert.Equal("EUR", product.Currency);
        Assert.True(product.IsActive);
    }

    [Fact]
    public void Create_WithZeroPriceAndInactiveStatus_Succeeds()
    {
        var id = Guid.Parse("22222222-2222-2222-2222-222222222222");

        var product = Product.Create(id, "Free Sample", 0m, "usd", false);

        Assert.Equal(id, product.Id);
        Assert.Equal("Free Sample", product.Name);
        Assert.Equal(0m, product.UnitPrice);
        Assert.Equal("USD", product.Currency);
        Assert.False(product.IsActive);
    }

    [Fact]
    public void Create_WithEmptyId_Throws()
    {
        var exception = Assert.Throws<ArgumentException>(
            () => Product.Create(Guid.Empty, "Widget", 10.00m, "EUR"));

        Assert.Equal("id", exception.ParamName);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithInvalidName_Throws(string? name)
    {
        var id = Guid.Parse("11111111-1111-1111-1111-111111111111");

        var exception = Assert.Throws<ArgumentException>(
            () => Product.Create(id, name!, 10.00m, "EUR"));

        Assert.Equal("name", exception.ParamName);
    }

    [Theory]
    [InlineData(200)]
    [InlineData(201)]
    public void Create_WithNameLengthBoundary_HandlesExpectedBehavior(int length)
    {
        var id = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var name = new string('A', length);

        if (length == 200)
        {
            var product = Product.Create(id, name, 10.00m, "EUR");

            Assert.Equal(200, product.Name.Length);
        }
        else
        {
            var exception = Assert.Throws<ArgumentException>(
                () => Product.Create(id, name, 10.00m, "EUR"));

            Assert.Equal("name", exception.ParamName);
        }
    }

    [Fact]
    public void Create_WithNegativeUnitPrice_Throws()
    {
        var id = Guid.Parse("11111111-1111-1111-1111-111111111111");

        var exception = Assert.Throws<ArgumentOutOfRangeException>(
            () => Product.Create(id, "Widget", -0.01m, "EUR"));

        Assert.Equal("unitPrice", exception.ParamName);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("EU")]
    [InlineData("EURO")]
    [InlineData("E1R")]
    public void Create_WithInvalidCurrency_Throws(string? currency)
    {
        var id = Guid.Parse("11111111-1111-1111-1111-111111111111");

        var exception = Assert.Throws<ArgumentException>(
            () => Product.Create(id, "Widget", 10.00m, currency!));

        Assert.Equal("currency", exception.ParamName);
    }
}
