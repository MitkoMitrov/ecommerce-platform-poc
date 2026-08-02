using Commerce.Api.Domain.Carts;
using Commerce.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Commerce.Api.Features.Carts;

public sealed class CartService
{
    private readonly CommerceDbContext _dbContext;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<CartService> _logger;

    public CartService(CommerceDbContext dbContext, TimeProvider timeProvider, ILogger<CartService> logger)
    {
        _dbContext = dbContext;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public async Task<Cart> CreateCartAsync(CancellationToken cancellationToken)
    {
        var cart = Cart.Create(_timeProvider.GetUtcNow());

        _dbContext.Carts.Add(cart);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Created cart {CartId}", cart.Id);

        return cart;
    }

    public async Task<Cart> GetCartAsync(Guid cartId, CancellationToken cancellationToken)
    {
        var cart = await _dbContext.Carts
            .AsNoTracking()
            .Include(c => c.Items)
            .FirstOrDefaultAsync(c => c.Id == cartId, cancellationToken);

        return cart ?? throw new KeyNotFoundException($"Cart '{cartId}' was not found.");
    }

    public async Task<Cart> AddItemAsync(
        Guid cartId,
        Guid productId,
        int quantity,
        CancellationToken cancellationToken)
    {
        var cart = await LoadTrackedCartAsync(cartId, cancellationToken);

        var product = await _dbContext.Products
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == productId && p.IsActive, cancellationToken)
            ?? throw new KeyNotFoundException($"Product '{productId}' was not found.");

        var now = _timeProvider.GetUtcNow();
        cart.AddItem(product.Id, product.Name, product.UnitPrice, product.Currency, quantity, now);

        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Added product {ProductId} to cart {CartId} with quantity {Quantity}",
            productId,
            cartId,
            quantity);

        return cart;
    }

    public async Task<Cart> UpdateItemQuantityAsync(
        Guid cartId,
        Guid productId,
        int quantity,
        CancellationToken cancellationToken)
    {
        var cart = await LoadTrackedCartAsync(cartId, cancellationToken);

        var now = _timeProvider.GetUtcNow();
        var updated = cart.UpdateQuantity(productId, quantity, now);
        if (!updated)
        {
            throw new KeyNotFoundException($"Cart item for product '{productId}' was not found in cart '{cartId}'.");
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Updated quantity for product {ProductId} in cart {CartId} to {Quantity}",
            productId,
            cartId,
            quantity);

        return cart;
    }

    public async Task RemoveItemAsync(
        Guid cartId,
        Guid productId,
        CancellationToken cancellationToken)
    {
        var cart = await LoadTrackedCartAsync(cartId, cancellationToken);

        var now = _timeProvider.GetUtcNow();
        var removed = cart.RemoveItem(productId, now);
        if (!removed)
        {
            throw new KeyNotFoundException($"Cart item for product '{productId}' was not found in cart '{cartId}'.");
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Removed product {ProductId} from cart {CartId}", productId, cartId);
    }

    private async Task<Cart> LoadTrackedCartAsync(Guid cartId, CancellationToken cancellationToken)
    {
        var cart = await _dbContext.Carts
            .Include(c => c.Items)
            .FirstOrDefaultAsync(c => c.Id == cartId, cancellationToken);

        return cart ?? throw new KeyNotFoundException($"Cart '{cartId}' was not found.");
    }
}
