namespace Commerce.Api.Features.Carts;

public sealed record CartResponse(
    Guid Id,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc,
    string? Currency,
    decimal Subtotal,
    IReadOnlyList<CartItemResponse> Items);

public sealed record CartItemResponse(
    Guid ProductId,
    string ProductName,
    decimal UnitPrice,
    string Currency,
    int Quantity,
    decimal LineTotal);

public sealed record AddCartItemRequest(Guid ProductId, int Quantity);

public sealed record UpdateCartItemRequest(int Quantity);
