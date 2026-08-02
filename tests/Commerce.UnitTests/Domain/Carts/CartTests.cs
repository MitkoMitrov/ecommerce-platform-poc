using Commerce.Api.Domain.Carts;

namespace Commerce.UnitTests.Domain.Carts;

public class CartTests
{
    [Fact]
    public void Create_InitializesEmptyCart()
    {
        var createdAtUtc = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

        var cart = Cart.Create(createdAtUtc);

        Assert.NotEqual(Guid.Empty, cart.Id);
        Assert.Equal(createdAtUtc, cart.CreatedAtUtc);
        Assert.Equal(createdAtUtc, cart.UpdatedAtUtc);
        Assert.Empty(cart.Items);
        Assert.Equal(0m, cart.Subtotal);
        Assert.Null(cart.Currency);
    }

    [Fact]
    public void Items_CannotBeCastToMutableList()
    {
        var createdAtUtc = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var cart = Cart.Create(createdAtUtc);

        Assert.Throws<InvalidCastException>(() => (List<CartItem>)(object)cart.Items);
    }

    [Fact]
    public void AddItem_WithValidNewProduct_AddsNormalizedSnapshotAndUpdatesTotals()
    {
        var createdAtUtc = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var updatedAtUtc = new DateTimeOffset(2026, 1, 2, 0, 0, 0, TimeSpan.Zero);
        var productId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var cart = Cart.Create(createdAtUtc);

        cart.AddItem(productId, "  Widget  ", 12.50m, "eur", 2, updatedAtUtc);

        var item = Assert.Single(cart.Items);
        Assert.Equal(cart.Id, item.CartId);
        Assert.Equal(productId, item.ProductId);
        Assert.Equal("Widget", item.ProductNameSnapshot);
        Assert.Equal(12.50m, item.UnitPriceSnapshot);
        Assert.Equal("EUR", item.Currency);
        Assert.Equal("EUR", cart.Currency);
        Assert.Equal(2, item.Quantity);
        Assert.Equal(25.00m, item.LineTotal);
        Assert.Equal(25.00m, cart.Subtotal);
        Assert.Equal(updatedAtUtc, cart.UpdatedAtUtc);
    }

    [Fact]
    public void AddItem_WithEmptyProductId_ThrowsAndLeavesCartUnchanged()
    {
        var createdAtUtc = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var updatedAtUtc = new DateTimeOffset(2026, 1, 2, 0, 0, 0, TimeSpan.Zero);
        var cart = Cart.Create(createdAtUtc);

        var exception = Assert.Throws<ArgumentException>(
            () => cart.AddItem(Guid.Empty, "Widget", 12.50m, "EUR", 1, updatedAtUtc));

        Assert.Equal("productId", exception.ParamName);
        AssertEmptyCartRemainsUnchanged(cart, createdAtUtc);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void AddItem_WithInvalidProductName_ThrowsAndLeavesCartUnchanged(string? productName)
    {
        var createdAtUtc = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var updatedAtUtc = new DateTimeOffset(2026, 1, 2, 0, 0, 0, TimeSpan.Zero);
        var productId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var cart = Cart.Create(createdAtUtc);

        var exception = Assert.Throws<ArgumentException>(
            () => cart.AddItem(productId, productName!, 12.50m, "EUR", 1, updatedAtUtc));

        Assert.Equal("productName", exception.ParamName);
        AssertEmptyCartRemainsUnchanged(cart, createdAtUtc);
    }

    [Fact]
    public void AddItem_WithProductNameLongerThan200Characters_ThrowsAndLeavesCartUnchanged()
    {
        var createdAtUtc = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var updatedAtUtc = new DateTimeOffset(2026, 1, 2, 0, 0, 0, TimeSpan.Zero);
        var productId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var cart = Cart.Create(createdAtUtc);
        var productName = new string('A', 201);

        var exception = Assert.Throws<ArgumentException>(
            () => cart.AddItem(productId, productName, 12.50m, "EUR", 1, updatedAtUtc));

        Assert.Equal("productName", exception.ParamName);
        AssertEmptyCartRemainsUnchanged(cart, createdAtUtc);
    }

    [Fact]
    public void AddItem_WithNegativeUnitPrice_ThrowsAndLeavesCartUnchanged()
    {
        var createdAtUtc = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var updatedAtUtc = new DateTimeOffset(2026, 1, 2, 0, 0, 0, TimeSpan.Zero);
        var productId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var cart = Cart.Create(createdAtUtc);

        var exception = Assert.Throws<ArgumentOutOfRangeException>(
            () => cart.AddItem(productId, "Widget", -0.01m, "EUR", 1, updatedAtUtc));

        Assert.Equal("unitPrice", exception.ParamName);
        AssertEmptyCartRemainsUnchanged(cart, createdAtUtc);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("EU")]
    [InlineData("EURO")]
    [InlineData("E1R")]
    public void AddItem_WithInvalidCurrency_ThrowsAndLeavesCartUnchanged(string? currency)
    {
        var createdAtUtc = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var updatedAtUtc = new DateTimeOffset(2026, 1, 2, 0, 0, 0, TimeSpan.Zero);
        var productId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var cart = Cart.Create(createdAtUtc);

        var exception = Assert.Throws<ArgumentException>(
            () => cart.AddItem(productId, "Widget", 12.50m, currency!, 1, updatedAtUtc));

        Assert.Equal("currency", exception.ParamName);
        AssertEmptyCartRemainsUnchanged(cart, createdAtUtc);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(100)]
    public void AddItem_WithQuantityOutsideAllowedRange_ThrowsAndLeavesCartUnchanged(int quantity)
    {
        var createdAtUtc = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var updatedAtUtc = new DateTimeOffset(2026, 1, 2, 0, 0, 0, TimeSpan.Zero);
        var productId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var cart = Cart.Create(createdAtUtc);

        var exception = Assert.Throws<ArgumentOutOfRangeException>(
            () => cart.AddItem(productId, "Widget", 12.50m, "EUR", quantity, updatedAtUtc));

        Assert.Equal("quantity", exception.ParamName);
        AssertEmptyCartRemainsUnchanged(cart, createdAtUtc);
    }

    [Fact]
    public void AddItem_WithTimestampBeforeCreation_ThrowsAndLeavesCartUnchanged()
    {
        var createdAtUtc = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var updatedAtUtc = createdAtUtc.AddSeconds(-1);
        var productId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var cart = Cart.Create(createdAtUtc);

        var exception = Assert.Throws<ArgumentOutOfRangeException>(
            () => cart.AddItem(productId, "Widget", 12.50m, "EUR", 1, updatedAtUtc));

        Assert.Equal("updatedAtUtc", exception.ParamName);
        AssertEmptyCartRemainsUnchanged(cart, createdAtUtc);
    }

    private static void AssertEmptyCartRemainsUnchanged(Cart cart, DateTimeOffset originalTimestamp)
    {
        Assert.Empty(cart.Items);
        Assert.Equal(0m, cart.Subtotal);
        Assert.Null(cart.Currency);
        Assert.Equal(originalTimestamp, cart.UpdatedAtUtc);
    }

    [Fact]
    public void AddItem_WithExistingProduct_MergesQuantityAndRefreshesSnapshot()
    {
        var createdAtUtc = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var firstUpdatedAtUtc = new DateTimeOffset(2026, 1, 2, 0, 0, 0, TimeSpan.Zero);
        var secondUpdatedAtUtc = new DateTimeOffset(2026, 1, 3, 0, 0, 0, TimeSpan.Zero);
        var productId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var cart = Cart.Create(createdAtUtc);
        cart.AddItem(productId, "Original Widget", 10.00m, "EUR", 2, firstUpdatedAtUtc);

        cart.AddItem(productId, "  Updated Widget  ", 12.50m, "eur", 3, secondUpdatedAtUtc);

        var item = Assert.Single(cart.Items);
        Assert.Equal(cart.Id, item.CartId);
        Assert.Equal(productId, item.ProductId);
        Assert.Equal("Updated Widget", item.ProductNameSnapshot);
        Assert.Equal(12.50m, item.UnitPriceSnapshot);
        Assert.Equal("EUR", item.Currency);
        Assert.Equal("EUR", cart.Currency);
        Assert.Equal(5, item.Quantity);
        Assert.Equal(62.50m, item.LineTotal);
        Assert.Equal(62.50m, cart.Subtotal);
        Assert.Equal(secondUpdatedAtUtc, cart.UpdatedAtUtc);
    }

    [Fact]
    public void AddItem_WithExistingProductResultingQuantityExactly99_Succeeds()
    {
        var createdAtUtc = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var firstUpdatedAtUtc = new DateTimeOffset(2026, 1, 2, 0, 0, 0, TimeSpan.Zero);
        var secondUpdatedAtUtc = new DateTimeOffset(2026, 1, 3, 0, 0, 0, TimeSpan.Zero);
        var productId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var cart = Cart.Create(createdAtUtc);
        cart.AddItem(productId, "Widget", 2.00m, "EUR", 98, firstUpdatedAtUtc);

        cart.AddItem(productId, "Widget Updated", 3.00m, "EUR", 1, secondUpdatedAtUtc);

        var item = Assert.Single(cart.Items);
        Assert.Equal(99, item.Quantity);
        Assert.Equal("Widget Updated", item.ProductNameSnapshot);
        Assert.Equal(3.00m, item.UnitPriceSnapshot);
        Assert.Equal(297.00m, item.LineTotal);
        Assert.Equal(297.00m, cart.Subtotal);
        Assert.Equal(secondUpdatedAtUtc, cart.UpdatedAtUtc);
    }

    [Fact]
    public void AddItem_WithExistingProductResultingQuantityAbove99_ThrowsAndLeavesExistingStateUnchanged()
    {
        var createdAtUtc = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var firstUpdatedAtUtc = new DateTimeOffset(2026, 1, 2, 0, 0, 0, TimeSpan.Zero);
        var secondUpdatedAtUtc = new DateTimeOffset(2026, 1, 3, 0, 0, 0, TimeSpan.Zero);
        var productId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var cart = Cart.Create(createdAtUtc);
        cart.AddItem(productId, "Original Widget", 10.00m, "EUR", 98, firstUpdatedAtUtc);

        var exception = Assert.Throws<ArgumentOutOfRangeException>(
            () => cart.AddItem(productId, "Changed Widget", 99.00m, "EUR", 2, secondUpdatedAtUtc));

        Assert.Equal("quantity", exception.ParamName);
        var item = Assert.Single(cart.Items);
        Assert.Equal("Original Widget", item.ProductNameSnapshot);
        Assert.Equal(10.00m, item.UnitPriceSnapshot);
        Assert.Equal("EUR", item.Currency);
        Assert.Equal(98, item.Quantity);
        Assert.Equal(980.00m, item.LineTotal);
        Assert.Equal(980.00m, cart.Subtotal);
        Assert.Equal(firstUpdatedAtUtc, cart.UpdatedAtUtc);
    }

    [Fact]
    public void AddItem_WithDifferentCurrencyForNewProduct_ThrowsAndLeavesExistingStateUnchanged()
    {
        var createdAtUtc = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var firstUpdatedAtUtc = new DateTimeOffset(2026, 1, 2, 0, 0, 0, TimeSpan.Zero);
        var secondUpdatedAtUtc = new DateTimeOffset(2026, 1, 3, 0, 0, 0, TimeSpan.Zero);
        var firstProductId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var secondProductId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        var cart = Cart.Create(createdAtUtc);
        cart.AddItem(firstProductId, "Widget", 10.00m, "EUR", 2, firstUpdatedAtUtc);

        Assert.Throws<InvalidOperationException>(
            () => cart.AddItem(secondProductId, "Other Product", 20.00m, "USD", 1, secondUpdatedAtUtc));

        var item = Assert.Single(cart.Items);
        Assert.Equal(firstProductId, item.ProductId);
        Assert.Equal("Widget", item.ProductNameSnapshot);
        Assert.Equal(10.00m, item.UnitPriceSnapshot);
        Assert.Equal("EUR", item.Currency);
        Assert.Equal(2, item.Quantity);
        Assert.Equal(20.00m, item.LineTotal);
        Assert.Equal(20.00m, cart.Subtotal);
        Assert.Equal("EUR", cart.Currency);
        Assert.Equal(firstUpdatedAtUtc, cart.UpdatedAtUtc);
    }

    [Fact]
    public void AddItem_WithDifferentCurrencyForExistingProduct_ThrowsAndLeavesExistingStateUnchanged()
    {
        var createdAtUtc = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var firstUpdatedAtUtc = new DateTimeOffset(2026, 1, 2, 0, 0, 0, TimeSpan.Zero);
        var secondUpdatedAtUtc = new DateTimeOffset(2026, 1, 3, 0, 0, 0, TimeSpan.Zero);
        var productId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var cart = Cart.Create(createdAtUtc);
        cart.AddItem(productId, "Original Widget", 10.00m, "EUR", 2, firstUpdatedAtUtc);

        Assert.Throws<InvalidOperationException>(
            () => cart.AddItem(productId, "Changed Widget", 99.00m, "USD", 3, secondUpdatedAtUtc));

        var item = Assert.Single(cart.Items);
        Assert.Equal("Original Widget", item.ProductNameSnapshot);
        Assert.Equal(10.00m, item.UnitPriceSnapshot);
        Assert.Equal("EUR", item.Currency);
        Assert.Equal(2, item.Quantity);
        Assert.Equal(20.00m, item.LineTotal);
        Assert.Equal(20.00m, cart.Subtotal);
        Assert.Equal("EUR", cart.Currency);
        Assert.Equal(firstUpdatedAtUtc, cart.UpdatedAtUtc);
    }

    [Fact]
    public void UpdateQuantity_WithExistingProduct_ReplacesQuantityAndUpdatesTotals()
    {
        var createdAtUtc = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var addedAtUtc = new DateTimeOffset(2026, 1, 2, 0, 0, 0, TimeSpan.Zero);
        var updatedAtUtc = new DateTimeOffset(2026, 1, 3, 0, 0, 0, TimeSpan.Zero);
        var productId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var cart = Cart.Create(createdAtUtc);
        cart.AddItem(productId, "Widget", 12.50m, "EUR", 2, addedAtUtc);

        var result = cart.UpdateQuantity(productId, 4, updatedAtUtc);

        Assert.True(result);
        var item = Assert.Single(cart.Items);
        Assert.Equal(productId, item.ProductId);
        Assert.Equal("Widget", item.ProductNameSnapshot);
        Assert.Equal(12.50m, item.UnitPriceSnapshot);
        Assert.Equal("EUR", item.Currency);
        Assert.Equal("EUR", cart.Currency);
        Assert.Equal(4, item.Quantity);
        Assert.Equal(50.00m, item.LineTotal);
        Assert.Equal(50.00m, cart.Subtotal);
        Assert.Equal(updatedAtUtc, cart.UpdatedAtUtc);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(99)]
    public void UpdateQuantity_WithAllowedBoundaryQuantity_Succeeds(int quantity)
    {
        var createdAtUtc = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var addedAtUtc = new DateTimeOffset(2026, 1, 2, 0, 0, 0, TimeSpan.Zero);
        var updatedAtUtc = new DateTimeOffset(2026, 1, 3, 0, 0, 0, TimeSpan.Zero);
        var productId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var cart = Cart.Create(createdAtUtc);
        cart.AddItem(productId, "Widget", 12.50m, "EUR", 2, addedAtUtc);

        var result = cart.UpdateQuantity(productId, quantity, updatedAtUtc);

        Assert.True(result);
        var item = Assert.Single(cart.Items);
        Assert.Equal(quantity, item.Quantity);
        Assert.Equal("Widget", item.ProductNameSnapshot);
        Assert.Equal(12.50m, item.UnitPriceSnapshot);
        Assert.Equal("EUR", item.Currency);
        Assert.Equal(12.50m * quantity, item.LineTotal);
        Assert.Equal(12.50m * quantity, cart.Subtotal);
        Assert.Equal(updatedAtUtc, cart.UpdatedAtUtc);
    }

    [Fact]
    public void UpdateQuantity_WithEmptyProductId_ThrowsAndLeavesExistingStateUnchanged()
    {
        var createdAtUtc = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var addedAtUtc = new DateTimeOffset(2026, 1, 2, 0, 0, 0, TimeSpan.Zero);
        var updatedAtUtc = new DateTimeOffset(2026, 1, 3, 0, 0, 0, TimeSpan.Zero);
        var productId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var cart = Cart.Create(createdAtUtc);
        cart.AddItem(productId, "Widget", 10.00m, "EUR", 2, addedAtUtc);

        var exception = Assert.Throws<ArgumentException>(
            () => cart.UpdateQuantity(Guid.Empty, 5, updatedAtUtc));

        Assert.Equal("productId", exception.ParamName);
        var item = Assert.Single(cart.Items);
        Assert.Equal(productId, item.ProductId);
        Assert.Equal("Widget", item.ProductNameSnapshot);
        Assert.Equal(10.00m, item.UnitPriceSnapshot);
        Assert.Equal("EUR", item.Currency);
        Assert.Equal(2, item.Quantity);
        Assert.Equal(20.00m, item.LineTotal);
        Assert.Equal(20.00m, cart.Subtotal);
        Assert.Equal("EUR", cart.Currency);
        Assert.Equal(addedAtUtc, cart.UpdatedAtUtc);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(100)]
    public void UpdateQuantity_WithQuantityOutsideAllowedRange_ThrowsAndLeavesExistingStateUnchanged(int quantity)
    {
        var createdAtUtc = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var addedAtUtc = new DateTimeOffset(2026, 1, 2, 0, 0, 0, TimeSpan.Zero);
        var updatedAtUtc = new DateTimeOffset(2026, 1, 3, 0, 0, 0, TimeSpan.Zero);
        var productId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var cart = Cart.Create(createdAtUtc);
        cart.AddItem(productId, "Widget", 10.00m, "EUR", 2, addedAtUtc);

        var exception = Assert.Throws<ArgumentOutOfRangeException>(
            () => cart.UpdateQuantity(productId, quantity, updatedAtUtc));

        Assert.Equal("quantity", exception.ParamName);
        var item = Assert.Single(cart.Items);
        Assert.Equal("Widget", item.ProductNameSnapshot);
        Assert.Equal(10.00m, item.UnitPriceSnapshot);
        Assert.Equal("EUR", item.Currency);
        Assert.Equal(2, item.Quantity);
        Assert.Equal(20.00m, item.LineTotal);
        Assert.Equal(20.00m, cart.Subtotal);
        Assert.Equal("EUR", cart.Currency);
        Assert.Equal(addedAtUtc, cart.UpdatedAtUtc);
    }

    [Fact]
    public void UpdateQuantity_WithTimestampBeforeCreation_ThrowsAndLeavesExistingStateUnchanged()
    {
        var createdAtUtc = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var addedAtUtc = new DateTimeOffset(2026, 1, 2, 0, 0, 0, TimeSpan.Zero);
        var updatedAtUtc = createdAtUtc.AddSeconds(-1);
        var productId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var cart = Cart.Create(createdAtUtc);
        cart.AddItem(productId, "Widget", 10.00m, "EUR", 2, addedAtUtc);

        var exception = Assert.Throws<ArgumentOutOfRangeException>(
            () => cart.UpdateQuantity(productId, 5, updatedAtUtc));

        Assert.Equal("updatedAtUtc", exception.ParamName);
        var item = Assert.Single(cart.Items);
        Assert.Equal("Widget", item.ProductNameSnapshot);
        Assert.Equal(10.00m, item.UnitPriceSnapshot);
        Assert.Equal("EUR", item.Currency);
        Assert.Equal(2, item.Quantity);
        Assert.Equal(20.00m, item.LineTotal);
        Assert.Equal(20.00m, cart.Subtotal);
        Assert.Equal("EUR", cart.Currency);
        Assert.Equal(addedAtUtc, cart.UpdatedAtUtc);
    }

    [Fact]
    public void UpdateQuantity_WithUnknownProduct_ReturnsFalseAndLeavesExistingStateUnchanged()
    {
        var createdAtUtc = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var addedAtUtc = new DateTimeOffset(2026, 1, 2, 0, 0, 0, TimeSpan.Zero);
        var updatedAtUtc = new DateTimeOffset(2026, 1, 3, 0, 0, 0, TimeSpan.Zero);
        var existingProductId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var unknownProductId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        var cart = Cart.Create(createdAtUtc);
        cart.AddItem(existingProductId, "Widget", 10.00m, "EUR", 2, addedAtUtc);

        var result = cart.UpdateQuantity(unknownProductId, 5, updatedAtUtc);

        Assert.False(result);
        var item = Assert.Single(cart.Items);
        Assert.Equal(existingProductId, item.ProductId);
        Assert.Equal("Widget", item.ProductNameSnapshot);
        Assert.Equal(10.00m, item.UnitPriceSnapshot);
        Assert.Equal("EUR", item.Currency);
        Assert.Equal(2, item.Quantity);
        Assert.Equal(20.00m, item.LineTotal);
        Assert.Equal(20.00m, cart.Subtotal);
        Assert.Equal("EUR", cart.Currency);
        Assert.Equal(addedAtUtc, cart.UpdatedAtUtc);
    }

    [Fact]
    public void RemoveItem_WithExistingProduct_RemovesOnlyTargetAndUpdatesTotals()
    {
        var createdAtUtc = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var firstAddedAtUtc = new DateTimeOffset(2026, 1, 2, 0, 0, 0, TimeSpan.Zero);
        var secondAddedAtUtc = new DateTimeOffset(2026, 1, 3, 0, 0, 0, TimeSpan.Zero);
        var removedAtUtc = new DateTimeOffset(2026, 1, 4, 0, 0, 0, TimeSpan.Zero);
        var firstProductId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var secondProductId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        var cart = Cart.Create(createdAtUtc);
        cart.AddItem(firstProductId, "First Widget", 10.00m, "EUR", 2, firstAddedAtUtc);
        cart.AddItem(secondProductId, "Second Widget", 5.00m, "EUR", 3, secondAddedAtUtc);

        var result = cart.RemoveItem(firstProductId, removedAtUtc);

        Assert.True(result);
        var item = Assert.Single(cart.Items);
        Assert.Equal(secondProductId, item.ProductId);
        Assert.Equal("Second Widget", item.ProductNameSnapshot);
        Assert.Equal(5.00m, item.UnitPriceSnapshot);
        Assert.Equal("EUR", item.Currency);
        Assert.Equal(3, item.Quantity);
        Assert.Equal(15.00m, item.LineTotal);
        Assert.Equal(15.00m, cart.Subtotal);
        Assert.Equal("EUR", cart.Currency);
        Assert.Equal(removedAtUtc, cart.UpdatedAtUtc);
        Assert.DoesNotContain(cart.Items, existingItem => existingItem.ProductId == firstProductId);
    }

    [Fact]
    public void RemoveItem_WithLastExistingProduct_EmptiesCartAndClearsDerivedValues()
    {
        var createdAtUtc = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var addedAtUtc = new DateTimeOffset(2026, 1, 2, 0, 0, 0, TimeSpan.Zero);
        var removedAtUtc = new DateTimeOffset(2026, 1, 3, 0, 0, 0, TimeSpan.Zero);
        var productId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var cart = Cart.Create(createdAtUtc);
        cart.AddItem(productId, "Widget", 12.50m, "EUR", 2, addedAtUtc);

        var result = cart.RemoveItem(productId, removedAtUtc);

        Assert.True(result);
        Assert.Empty(cart.Items);
        Assert.Equal(0m, cart.Subtotal);
        Assert.Null(cart.Currency);
        Assert.Equal(removedAtUtc, cart.UpdatedAtUtc);
    }

    [Fact]
    public void RemoveItem_WithEmptyProductId_ThrowsAndLeavesExistingStateUnchanged()
    {
        var createdAtUtc = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var addedAtUtc = new DateTimeOffset(2026, 1, 2, 0, 0, 0, TimeSpan.Zero);
        var attemptedRemovedAtUtc = new DateTimeOffset(2026, 1, 3, 0, 0, 0, TimeSpan.Zero);
        var productId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var cart = Cart.Create(createdAtUtc);
        cart.AddItem(productId, "Widget", 10.00m, "EUR", 2, addedAtUtc);

        var exception = Assert.Throws<ArgumentException>(
            () => cart.RemoveItem(Guid.Empty, attemptedRemovedAtUtc));

        Assert.Equal("productId", exception.ParamName);
        var item = Assert.Single(cart.Items);
        Assert.Equal(productId, item.ProductId);
        Assert.Equal("Widget", item.ProductNameSnapshot);
        Assert.Equal(10.00m, item.UnitPriceSnapshot);
        Assert.Equal("EUR", item.Currency);
        Assert.Equal(2, item.Quantity);
        Assert.Equal(20.00m, item.LineTotal);
        Assert.Equal(20.00m, cart.Subtotal);
        Assert.Equal("EUR", cart.Currency);
        Assert.Equal(addedAtUtc, cart.UpdatedAtUtc);
    }

    [Fact]
    public void RemoveItem_WithTimestampBeforeCreation_ThrowsAndLeavesExistingStateUnchanged()
    {
        var createdAtUtc = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var addedAtUtc = new DateTimeOffset(2026, 1, 2, 0, 0, 0, TimeSpan.Zero);
        var invalidRemovedAtUtc = createdAtUtc.AddSeconds(-1);
        var productId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var cart = Cart.Create(createdAtUtc);
        cart.AddItem(productId, "Widget", 10.00m, "EUR", 2, addedAtUtc);

        var exception = Assert.Throws<ArgumentOutOfRangeException>(
            () => cart.RemoveItem(productId, invalidRemovedAtUtc));

        Assert.Equal("updatedAtUtc", exception.ParamName);
        var item = Assert.Single(cart.Items);
        Assert.Equal("Widget", item.ProductNameSnapshot);
        Assert.Equal(10.00m, item.UnitPriceSnapshot);
        Assert.Equal("EUR", item.Currency);
        Assert.Equal(2, item.Quantity);
        Assert.Equal(20.00m, item.LineTotal);
        Assert.Equal(20.00m, cart.Subtotal);
        Assert.Equal("EUR", cart.Currency);
        Assert.Equal(addedAtUtc, cart.UpdatedAtUtc);
    }

    [Fact]
    public void RemoveItem_WithUnknownProduct_ReturnsFalseAndLeavesExistingStateUnchanged()
    {
        var createdAtUtc = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var addedAtUtc = new DateTimeOffset(2026, 1, 2, 0, 0, 0, TimeSpan.Zero);
        var attemptedRemovedAtUtc = new DateTimeOffset(2026, 1, 3, 0, 0, 0, TimeSpan.Zero);
        var existingProductId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var unknownProductId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        var cart = Cart.Create(createdAtUtc);
        cart.AddItem(existingProductId, "Widget", 10.00m, "EUR", 2, addedAtUtc);

        var result = cart.RemoveItem(unknownProductId, attemptedRemovedAtUtc);

        Assert.False(result);
        var item = Assert.Single(cart.Items);
        Assert.Equal(existingProductId, item.ProductId);
        Assert.Equal("Widget", item.ProductNameSnapshot);
        Assert.Equal(10.00m, item.UnitPriceSnapshot);
        Assert.Equal("EUR", item.Currency);
        Assert.Equal(2, item.Quantity);
        Assert.Equal(20.00m, item.LineTotal);
        Assert.Equal(20.00m, cart.Subtotal);
        Assert.Equal("EUR", cart.Currency);
        Assert.Equal(addedAtUtc, cart.UpdatedAtUtc);
    }
}
