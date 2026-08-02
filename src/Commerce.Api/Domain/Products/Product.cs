namespace Commerce.Api.Domain.Products;

public sealed class Product
{
    private const int MaxNameLength = 200;
    private const int CurrencyCodeLength = 3;

    private Product()
    {
    }

    public Guid Id { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public decimal UnitPrice { get; private set; }

    public string Currency { get; private set; } = string.Empty;

    public bool IsActive { get; private set; }

    public static Product Create(Guid id, string name, decimal unitPrice, string currency, bool isActive = true)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Product id must not be empty.", nameof(id));
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Product name must not be empty or whitespace.", nameof(name));
        }

        var trimmedName = name.Trim();
        if (trimmedName.Length > MaxNameLength)
        {
            throw new ArgumentException(
                $"Product name must not exceed {MaxNameLength} characters.", nameof(name));
        }

        if (unitPrice < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(unitPrice), unitPrice, "Unit price must be greater than or equal to zero.");
        }

        var normalizedCurrency = NormalizeCurrency(currency);

        return new Product
        {
            Id = id,
            Name = trimmedName,
            UnitPrice = unitPrice,
            Currency = normalizedCurrency,
            IsActive = isActive
        };
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
