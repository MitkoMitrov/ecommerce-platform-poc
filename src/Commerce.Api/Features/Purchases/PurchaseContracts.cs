using Commerce.Api.Features.Carts;

namespace Commerce.Api.Features.Purchases;

public sealed record PurchaseResponse(
    Guid Id,
    Guid CartId,
    DateTimeOffset PurchasedAtUtc,
    string Currency,
    decimal Total,
    IReadOnlyList<PurchaseItemResponse> Items);

public sealed record PurchaseItemResponse(
    Guid ProductId,
    string ProductName,
    decimal UnitPrice,
    string Currency,
    int Quantity,
    decimal LineTotal);

public sealed record PurchaseCartResponse(PurchaseResponse Purchase, CartResponse Cart);
