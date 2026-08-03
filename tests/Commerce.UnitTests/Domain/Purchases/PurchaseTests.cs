using Commerce.Api.Domain.Purchases;

namespace Commerce.UnitTests.Domain.Purchases;

public class PurchaseTests
{
    [Fact]
    public void Create_WithValidItems_CreatesSnapshotWithAuthoritativeTotalsAndCurrency()
    {
        var cartId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var purchasedAtUtc = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var firstProductId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var secondProductId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        var items = new List<(Guid ProductId, string ProductName, decimal UnitPrice, string Currency, int Quantity)>
        {
            (firstProductId, "Wireless Mouse", 29.99m, "eur", 2),
            (secondProductId, "USB-C Hub", 49.50m, "EUR", 1),
        };

        var purchase = Purchase.Create(cartId, items, purchasedAtUtc);

        Assert.NotEqual(Guid.Empty, purchase.Id);
        Assert.Equal(cartId, purchase.CartId);
        Assert.Equal(purchasedAtUtc, purchase.PurchasedAtUtc);
        Assert.Equal("EUR", purchase.Currency);
        Assert.Equal(109.48m, purchase.Total);
        Assert.Equal(2, purchase.Items.Count);

        var mouse = purchase.Items.Single(item => item.ProductId == firstProductId);
        Assert.Equal("Wireless Mouse", mouse.ProductName);
        Assert.Equal(29.99m, mouse.UnitPrice);
        Assert.Equal("EUR", mouse.Currency);
        Assert.Equal(2, mouse.Quantity);
        Assert.Equal(59.98m, mouse.LineTotal);

        var hub = purchase.Items.Single(item => item.ProductId == secondProductId);
        Assert.Equal("USB-C Hub", hub.ProductName);
        Assert.Equal(49.50m, hub.UnitPrice);
        Assert.Equal(1, hub.Quantity);
        Assert.Equal(49.50m, hub.LineTotal);
    }

    [Fact]
    public void Create_TrimsProductNameAndNormalizesCurrency()
    {
        var cartId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var productId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var purchasedAtUtc = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var items = new List<(Guid ProductId, string ProductName, decimal UnitPrice, string Currency, int Quantity)>
        {
            (productId, "  Widget  ", 12.50m, "eur", 3),
        };

        var purchase = Purchase.Create(cartId, items, purchasedAtUtc);

        var item = Assert.Single(purchase.Items);
        Assert.Equal("Widget", item.ProductName);
        Assert.Equal("EUR", item.Currency);
        Assert.Equal("EUR", purchase.Currency);
    }

    [Fact]
    public void Create_WithNoItems_ThrowsAndCannotProduceAPurchase()
    {
        var cartId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var purchasedAtUtc = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var items = new List<(Guid ProductId, string ProductName, decimal UnitPrice, string Currency, int Quantity)>();

        var exception = Assert.Throws<ArgumentException>(() => Purchase.Create(cartId, items, purchasedAtUtc));

        Assert.Equal("items", exception.ParamName);
    }

    [Fact]
    public void Create_WithEmptyCartId_Throws()
    {
        var purchasedAtUtc = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var items = new List<(Guid ProductId, string ProductName, decimal UnitPrice, string Currency, int Quantity)>
        {
            (Guid.Parse("11111111-1111-1111-1111-111111111111"), "Widget", 12.50m, "EUR", 1),
        };

        var exception = Assert.Throws<ArgumentException>(() => Purchase.Create(Guid.Empty, items, purchasedAtUtc));

        Assert.Equal("cartId", exception.ParamName);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithInvalidProductName_Throws(string? productName)
    {
        var cartId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var purchasedAtUtc = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var items = new List<(Guid ProductId, string ProductName, decimal UnitPrice, string Currency, int Quantity)>
        {
            (Guid.Parse("11111111-1111-1111-1111-111111111111"), productName!, 12.50m, "EUR", 1),
        };

        Assert.Throws<ArgumentException>(() => Purchase.Create(cartId, items, purchasedAtUtc));
    }

    [Fact]
    public void Create_WithNegativeUnitPrice_Throws()
    {
        var cartId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var purchasedAtUtc = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var items = new List<(Guid ProductId, string ProductName, decimal UnitPrice, string Currency, int Quantity)>
        {
            (Guid.Parse("11111111-1111-1111-1111-111111111111"), "Widget", -0.01m, "EUR", 1),
        };

        Assert.Throws<ArgumentOutOfRangeException>(() => Purchase.Create(cartId, items, purchasedAtUtc));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(100)]
    public void Create_WithQuantityOutsideAllowedRange_Throws(int quantity)
    {
        var cartId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var purchasedAtUtc = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var items = new List<(Guid ProductId, string ProductName, decimal UnitPrice, string Currency, int Quantity)>
        {
            (Guid.Parse("11111111-1111-1111-1111-111111111111"), "Widget", 12.50m, "EUR", quantity),
        };

        Assert.Throws<ArgumentOutOfRangeException>(() => Purchase.Create(cartId, items, purchasedAtUtc));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("EU")]
    [InlineData("EURO")]
    [InlineData("E1R")]
    public void Create_WithInvalidCurrency_Throws(string? currency)
    {
        var cartId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var purchasedAtUtc = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var items = new List<(Guid ProductId, string ProductName, decimal UnitPrice, string Currency, int Quantity)>
        {
            (Guid.Parse("11111111-1111-1111-1111-111111111111"), "Widget", 12.50m, currency!, 1),
        };

        Assert.Throws<ArgumentException>(() => Purchase.Create(cartId, items, purchasedAtUtc));
    }

    [Fact]
    public void Items_CannotBeCastToMutableList()
    {
        var cartId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var purchasedAtUtc = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var items = new List<(Guid ProductId, string ProductName, decimal UnitPrice, string Currency, int Quantity)>
        {
            (Guid.Parse("11111111-1111-1111-1111-111111111111"), "Widget", 12.50m, "EUR", 1),
        };

        var purchase = Purchase.Create(cartId, items, purchasedAtUtc);

        Assert.Throws<InvalidCastException>(() => (List<PurchaseItem>)(object)purchase.Items);
    }
}
