namespace Commerce.Api.Domain.Carts;

public sealed class CartItem
{
    private CartItem()
    {
    }

    private CartItem(
        Guid cartId,
        Guid productId,
        string productNameSnapshot,
        decimal unitPriceSnapshot,
        string currency,
        int quantity)
    {
        CartId = cartId;
        ProductId = productId;
        ProductNameSnapshot = productNameSnapshot;
        UnitPriceSnapshot = unitPriceSnapshot;
        Currency = currency;
        Quantity = quantity;
    }

    public Guid CartId { get; private set; }

    public Guid ProductId { get; private set; }

    public string ProductNameSnapshot { get; private set; } = string.Empty;

    public decimal UnitPriceSnapshot { get; private set; }

    public string Currency { get; private set; } = string.Empty;

    public int Quantity { get; private set; }

    public decimal LineTotal => UnitPriceSnapshot * Quantity;

    internal static CartItem Create(
        Guid cartId,
        Guid productId,
        string productNameSnapshot,
        decimal unitPriceSnapshot,
        string currency,
        int quantity)
    {
        return new CartItem(cartId, productId, productNameSnapshot, unitPriceSnapshot, currency, quantity);
    }

    internal void ReplaceQuantity(int quantity)
    {
        Quantity = quantity;
    }

    internal void RefreshSnapshot(string productNameSnapshot, decimal unitPriceSnapshot)
    {
        ProductNameSnapshot = productNameSnapshot;
        UnitPriceSnapshot = unitPriceSnapshot;
    }
}
