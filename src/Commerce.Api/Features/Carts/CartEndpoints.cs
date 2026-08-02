namespace Commerce.Api.Features.Carts;

public static class CartEndpoints
{
    public static RouteGroupBuilder MapCartEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/carts").WithTags("Carts");

        group.MapPost("/", CreateCartAsync)
            .WithName("CreateCart")
            .Produces<CartResponse>(StatusCodes.Status201Created);

        group.MapGet("/{cartId:guid}", GetCartAsync)
            .WithName("GetCart")
            .Produces<CartResponse>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPost("/{cartId:guid}/items", AddCartItemAsync)
            .WithName("AddCartItem")
            .Produces<CartResponse>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPut("/{cartId:guid}/items/{productId:guid}", UpdateCartItemQuantityAsync)
            .WithName("UpdateCartItemQuantity")
            .Produces<CartResponse>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapDelete("/{cartId:guid}/items/{productId:guid}", RemoveCartItemAsync)
            .WithName("RemoveCartItem")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound);

        return group;
    }

    private static async Task<IResult> CreateCartAsync(
        CartService cartService,
        CancellationToken cancellationToken)
    {
        var cart = await cartService.CreateCartAsync(cancellationToken);
        var response = CartMapper.ToResponse(cart);
        return Results.Created($"/api/carts/{response.Id}", response);
    }

    private static async Task<IResult> GetCartAsync(
        Guid cartId,
        CartService cartService,
        CancellationToken cancellationToken)
    {
        ValidateId(cartId, nameof(cartId));

        var cart = await cartService.GetCartAsync(cartId, cancellationToken);
        return Results.Ok(CartMapper.ToResponse(cart));
    }

    private static async Task<IResult> AddCartItemAsync(
        Guid cartId,
        AddCartItemRequest request,
        CartService cartService,
        CancellationToken cancellationToken)
    {
        ValidateId(cartId, nameof(cartId));
        ValidateId(request.ProductId, nameof(request.ProductId));
        ValidateQuantity(request.Quantity);

        var cart = await cartService.AddItemAsync(cartId, request.ProductId, request.Quantity, cancellationToken);
        return Results.Ok(CartMapper.ToResponse(cart));
    }

    private static async Task<IResult> UpdateCartItemQuantityAsync(
        Guid cartId,
        Guid productId,
        UpdateCartItemRequest request,
        CartService cartService,
        CancellationToken cancellationToken)
    {
        ValidateId(cartId, nameof(cartId));
        ValidateId(productId, nameof(productId));
        ValidateQuantity(request.Quantity);

        var cart = await cartService.UpdateItemQuantityAsync(cartId, productId, request.Quantity, cancellationToken);
        return Results.Ok(CartMapper.ToResponse(cart));
    }

    private static async Task<IResult> RemoveCartItemAsync(
        Guid cartId,
        Guid productId,
        CartService cartService,
        CancellationToken cancellationToken)
    {
        ValidateId(cartId, nameof(cartId));
        ValidateId(productId, nameof(productId));

        await cartService.RemoveItemAsync(cartId, productId, cancellationToken);
        return Results.NoContent();
    }

    private static void ValidateId(Guid id, string paramName)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException($"'{paramName}' must not be an empty GUID.", paramName);
        }
    }

    private static void ValidateQuantity(int quantity)
    {
        if (quantity is < 1 or > 99)
        {
            throw new ArgumentOutOfRangeException(nameof(quantity), quantity, "Quantity must be between 1 and 99 inclusive.");
        }
    }
}
