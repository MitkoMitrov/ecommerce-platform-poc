using Commerce.Api.Domain.Carts;

namespace Commerce.Api.Features.Carts;

internal static class CartMapper
{
    public static CartResponse ToResponse(Cart cart)
    {
        var items = cart.Items
            .OrderBy(item => item.ProductNameSnapshot, StringComparer.Ordinal)
            .ThenBy(item => item.ProductId)
            .Select(item => new CartItemResponse(
                item.ProductId,
                item.ProductNameSnapshot,
                item.UnitPriceSnapshot,
                item.Currency,
                item.Quantity,
                item.LineTotal))
            .ToList();

        return new CartResponse(
            cart.Id,
            cart.CreatedAtUtc,
            cart.UpdatedAtUtc,
            cart.Currency,
            cart.Subtotal,
            items);
    }
}
