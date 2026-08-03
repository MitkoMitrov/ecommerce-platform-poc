namespace Commerce.Api.Domain.Carts;

public sealed class Cart
{
    private const int MaxProductNameLength = 200;
    private const int MinQuantity = 1;
    private const int MaxQuantity = 99;
    private const int CurrencyCodeLength = 3;

    private readonly List<CartItem> _items = new();

    private Cart()
    {
    }

    public Guid Id { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public DateTimeOffset UpdatedAtUtc { get; private set; }

    public IReadOnlyList<CartItem> Items => _items.AsReadOnly();

    public decimal Subtotal => _items.Sum(item => item.LineTotal);

    public string? Currency => _items.Count == 0 ? null : _items[0].Currency;

    public static Cart Create(DateTimeOffset createdAtUtc)
    {
        return new Cart
        {
            Id = Guid.NewGuid(),
            CreatedAtUtc = createdAtUtc,
            UpdatedAtUtc = createdAtUtc
        };
    }

    public void AddItem(
        Guid productId,
        string productName,
        decimal unitPrice,
        string currency,
        int quantity,
        DateTimeOffset updatedAtUtc)
    {
        ValidateProductId(productId);

        if (string.IsNullOrWhiteSpace(productName))
        {
            throw new ArgumentException("Product name must not be empty or whitespace.", nameof(productName));
        }

        var trimmedProductName = productName.Trim();
        if (trimmedProductName.Length > MaxProductNameLength)
        {
            throw new ArgumentException(
                $"Product name must not exceed {MaxProductNameLength} characters.", nameof(productName));
        }

        if (unitPrice < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(unitPrice), unitPrice, "Unit price must be greater than or equal to zero.");
        }

        var normalizedCurrency = NormalizeCurrency(currency);

        ValidateQuantity(quantity);
        ValidateUpdatedAtUtc(updatedAtUtc);

        if (Currency is not null && !string.Equals(Currency, normalizedCurrency, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"Cannot add an item in currency '{normalizedCurrency}' to a cart already using currency '{Currency}'.");
        }

        var existingItem = FindItem(productId);
        if (existingItem is null)
        {
            _items.Add(CartItem.Create(Id, productId, trimmedProductName, unitPrice, normalizedCurrency, quantity));
        }
        else
        {
            var resultingQuantity = existingItem.Quantity + quantity;
            if (resultingQuantity > MaxQuantity)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(quantity), quantity, $"Resulting quantity must not exceed {MaxQuantity}.");
            }

            existingItem.RefreshSnapshot(trimmedProductName, unitPrice);
            existingItem.ReplaceQuantity(resultingQuantity);
        }

        UpdatedAtUtc = updatedAtUtc;
    }

    public bool UpdateQuantity(
        Guid productId,
        int quantity,
        DateTimeOffset updatedAtUtc)
    {
        ValidateProductId(productId);
        ValidateQuantity(quantity);
        ValidateUpdatedAtUtc(updatedAtUtc);

        var existingItem = FindItem(productId);
        if (existingItem is null)
        {
            return false;
        }

        existingItem.ReplaceQuantity(quantity);
        UpdatedAtUtc = updatedAtUtc;
        return true;
    }

    public bool RemoveItem(
        Guid productId,
        DateTimeOffset updatedAtUtc)
    {
        ValidateProductId(productId);
        ValidateUpdatedAtUtc(updatedAtUtc);

        var existingItem = FindItem(productId);
        if (existingItem is null)
        {
            return false;
        }

        _items.Remove(existingItem);
        UpdatedAtUtc = updatedAtUtc;
        return true;
    }

    public void ClearAfterPurchase(DateTimeOffset updatedAtUtc)
    {
        ValidateUpdatedAtUtc(updatedAtUtc);

        if (_items.Count == 0)
        {
            throw new InvalidOperationException($"Cart '{Id}' has no items and cannot be purchased.");
        }

        _items.Clear();
        UpdatedAtUtc = updatedAtUtc;
    }

    private CartItem? FindItem(Guid productId)
    {
        return _items.Find(item => item.ProductId == productId);
    }

    private static void ValidateProductId(Guid productId)
    {
        if (productId == Guid.Empty)
        {
            throw new ArgumentException("Product id must not be empty.", nameof(productId));
        }
    }

    private static void ValidateQuantity(int quantity)
    {
        if (quantity < MinQuantity || quantity > MaxQuantity)
        {
            throw new ArgumentOutOfRangeException(
                nameof(quantity), quantity, $"Quantity must be between {MinQuantity} and {MaxQuantity} inclusive.");
        }
    }

    private void ValidateUpdatedAtUtc(DateTimeOffset updatedAtUtc)
    {
        if (updatedAtUtc < CreatedAtUtc)
        {
            throw new ArgumentOutOfRangeException(
                nameof(updatedAtUtc), updatedAtUtc, "Updated timestamp must not be earlier than the cart's creation timestamp.");
        }
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
