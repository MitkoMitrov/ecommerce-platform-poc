namespace Commerce.Api.Features.Products;

public sealed record ProductResponse(Guid Id, string Name, decimal UnitPrice, string Currency);
