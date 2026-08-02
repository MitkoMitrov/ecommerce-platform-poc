using Commerce.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Commerce.Api.Features.Products;

public static class ProductEndpoints
{
    public static RouteGroupBuilder MapProductEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/products").WithTags("Products");

        group.MapGet("/", GetProductsAsync)
            .WithName("GetProducts")
            .Produces<IReadOnlyList<ProductResponse>>();

        return group;
    }

    private static async Task<IResult> GetProductsAsync(
        CommerceDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var products = await dbContext.Products
            .AsNoTracking()
            .Where(product => product.IsActive)
            .OrderBy(product => product.Name)
            .ThenBy(product => product.Id)
            .Select(product => new ProductResponse(product.Id, product.Name, product.UnitPrice, product.Currency))
            .ToListAsync(cancellationToken);

        return Results.Ok(products);
    }
}
