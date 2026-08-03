using Commerce.Api.Features.Carts;

namespace Commerce.Api.Features.Purchases;

public static class PurchaseEndpoints
{
    public static void MapPurchaseEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var cartsGroup = endpoints.MapGroup("/api/carts").WithTags("Purchases");

        cartsGroup.MapPost("/{cartId:guid}/purchase", PurchaseCartAsync)
            .WithName("PurchaseCart")
            .Produces<PurchaseCartResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        var purchasesGroup = endpoints.MapGroup("/api/purchases").WithTags("Purchases");

        purchasesGroup.MapGet("/", GetPurchaseHistoryAsync)
            .WithName("GetPurchaseHistory")
            .Produces<IReadOnlyList<PurchaseResponse>>();

        purchasesGroup.MapGet("/{purchaseId:guid}", GetPurchaseAsync)
            .WithName("GetPurchase")
            .Produces<PurchaseResponse>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound);
    }

    private static async Task<IResult> PurchaseCartAsync(
        Guid cartId,
        PurchaseService purchaseService,
        CancellationToken cancellationToken)
    {
        ValidateId(cartId, nameof(cartId));

        var (purchase, cart) = await purchaseService.PurchaseCartAsync(cartId, cancellationToken);
        var response = new PurchaseCartResponse(PurchaseMapper.ToResponse(purchase), CartMapper.ToResponse(cart));

        return Results.Created($"/api/purchases/{purchase.Id}", response);
    }

    private static async Task<IResult> GetPurchaseHistoryAsync(
        PurchaseService purchaseService,
        CancellationToken cancellationToken)
    {
        var purchases = await purchaseService.GetPurchaseHistoryAsync(cancellationToken);
        var response = purchases.Select(PurchaseMapper.ToResponse).ToList();

        return Results.Ok(response);
    }

    private static async Task<IResult> GetPurchaseAsync(
        Guid purchaseId,
        PurchaseService purchaseService,
        CancellationToken cancellationToken)
    {
        ValidateId(purchaseId, nameof(purchaseId));

        var purchase = await purchaseService.GetPurchaseAsync(purchaseId, cancellationToken);
        return Results.Ok(PurchaseMapper.ToResponse(purchase));
    }

    private static void ValidateId(Guid id, string paramName)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException($"'{paramName}' must not be an empty GUID.", paramName);
        }
    }
}
