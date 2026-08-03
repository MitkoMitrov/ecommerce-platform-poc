namespace Commerce.Api.Domain.Purchases;

public sealed class PurchaseItem
{
    private PurchaseItem()
    {
    }

    private PurchaseItem(
        Guid purchaseId,
        Guid productId,
        string productName,
        decimal unitPrice,
        string currency,
        int quantity)
    {
        PurchaseId = purchaseId;
        ProductId = productId;
        ProductName = productName;
        UnitPrice = unitPrice;
        Currency = currency;
        Quantity = quantity;
    }

    public Guid PurchaseId { get; private set; }

    public Guid ProductId { get; private set; }

    public string ProductName { get; private set; } = string.Empty;

    public decimal UnitPrice { get; private set; }

    public string Currency { get; private set; } = string.Empty;

    public int Quantity { get; private set; }

    public decimal LineTotal => UnitPrice * Quantity;

    internal static PurchaseItem Create(
        Guid purchaseId,
        Guid productId,
        string productName,
        decimal unitPrice,
        string currency,
        int quantity)
    {
        return new PurchaseItem(purchaseId, productId, productName, unitPrice, currency, quantity);
    }
}
