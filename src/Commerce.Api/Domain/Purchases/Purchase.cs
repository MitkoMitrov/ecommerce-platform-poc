namespace Commerce.Api.Domain.Purchases;

public sealed class Purchase
{
    private const int MaxProductNameLength = 200;
    private const int MinQuantity = 1;
    private const int MaxQuantity = 99;
    private const int CurrencyCodeLength = 3;

    private readonly List<PurchaseItem> _items = new();

    private Purchase()
    {
    }

    public Guid Id { get; private set; }

    public Guid CartId { get; private set; }

    public DateTimeOffset PurchasedAtUtc { get; private set; }

    public string Currency { get; private set; } = string.Empty;

    public decimal Total { get; private set; }

    public IReadOnlyList<PurchaseItem> Items => _items.AsReadOnly();

    public static Purchase Create(
        Guid cartId,
        IReadOnlyList<(Guid ProductId, string ProductName, decimal UnitPrice, string Currency, int Quantity)> items,
        DateTimeOffset purchasedAtUtc)
    {
        if (cartId == Guid.Empty)
        {
            throw new ArgumentException("Cart id must not be empty.", nameof(cartId));
        }

        if (items is null || items.Count == 0)
        {
            throw new ArgumentException("A Purchase must contain at least one item.", nameof(items));
        }

        var purchase = new Purchase
        {
            Id = Guid.NewGuid(),
            CartId = cartId,
            PurchasedAtUtc = purchasedAtUtc,
        };

        string? currency = null;
        var total = 0m;

        foreach (var item in items)
        {
            var purchaseItem = CreateValidatedItem(purchase.Id, item);

            if (currency is null)
            {
                currency = purchaseItem.Currency;
            }
            else if (!string.Equals(currency, purchaseItem.Currency, StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    $"Cannot create a Purchase with items in mixed currencies ('{currency}' and '{purchaseItem.Currency}').");
            }

            total += purchaseItem.LineTotal;
            purchase._items.Add(purchaseItem);
        }

        purchase.Currency = currency!;
        purchase.Total = total;

        return purchase;
    }

    private static PurchaseItem CreateValidatedItem(
        Guid purchaseId,
        (Guid ProductId, string ProductName, decimal UnitPrice, string Currency, int Quantity) item)
    {
        if (item.ProductId == Guid.Empty)
        {
            throw new ArgumentException("Product id must not be empty.", nameof(item.ProductId));
        }

        if (string.IsNullOrWhiteSpace(item.ProductName))
        {
            throw new ArgumentException("Product name must not be empty or whitespace.", nameof(item.ProductName));
        }

        var trimmedProductName = item.ProductName.Trim();
        if (trimmedProductName.Length > MaxProductNameLength)
        {
            throw new ArgumentException(
                $"Product name must not exceed {MaxProductNameLength} characters.", nameof(item.ProductName));
        }

        if (item.UnitPrice < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(item.UnitPrice), item.UnitPrice, "Unit price must be greater than or equal to zero.");
        }

        if (item.Quantity < MinQuantity || item.Quantity > MaxQuantity)
        {
            throw new ArgumentOutOfRangeException(
                nameof(item.Quantity), item.Quantity, $"Quantity must be between {MinQuantity} and {MaxQuantity} inclusive.");
        }

        var normalizedCurrency = NormalizeCurrency(item.Currency);

        return PurchaseItem.Create(purchaseId, item.ProductId, trimmedProductName, item.UnitPrice, normalizedCurrency, item.Quantity);
    }

    private static string NormalizeCurrency(string currency)
    {
        if (string.IsNullOrWhiteSpace(currency) || currency.Length != CurrencyCodeLength || !currency.All(char.IsLetter))
        {
            throw new ArgumentException(
                $"Currency must contain exactly {CurrencyCodeLength} alphabetic characters.", nameof(currency));
        }

        return currency.ToUpperInvariant();
    }
}
