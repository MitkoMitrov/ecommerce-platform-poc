using Commerce.Api.Domain.Purchases;

namespace Commerce.Api.Features.Purchases;

internal static class PurchaseMapper
{
    public static PurchaseResponse ToResponse(Purchase purchase)
    {
        var items = purchase.Items
            .OrderBy(item => item.ProductName, StringComparer.Ordinal)
            .ThenBy(item => item.ProductId)
            .Select(item => new PurchaseItemResponse(
                item.ProductId,
                item.ProductName,
                item.UnitPrice,
                item.Currency,
                item.Quantity,
                item.LineTotal))
            .ToList();

        return new PurchaseResponse(
            purchase.Id,
            purchase.CartId,
            purchase.PurchasedAtUtc,
            purchase.Currency,
            purchase.Total,
            items);
    }
}
