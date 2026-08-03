using Commerce.Api.Domain.Carts;
using Commerce.Api.Domain.Purchases;
using Commerce.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Commerce.Api.Features.Purchases;

public sealed class PurchaseService
{
    private readonly CommerceDbContext _dbContext;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<PurchaseService> _logger;

    public PurchaseService(CommerceDbContext dbContext, TimeProvider timeProvider, ILogger<PurchaseService> logger)
    {
        _dbContext = dbContext;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public async Task<(Purchase Purchase, Cart Cart)> PurchaseCartAsync(Guid cartId, CancellationToken cancellationToken)
    {
        var cart = await _dbContext.Carts
            .Include(c => c.Items)
            .FirstOrDefaultAsync(c => c.Id == cartId, cancellationToken);

        if (cart is null)
        {
            throw new KeyNotFoundException($"Cart '{cartId}' was not found.");
        }

        if (cart.Items.Count == 0)
        {
            throw new InvalidOperationException($"Cart '{cartId}' is empty and cannot be purchased.");
        }

        var now = _timeProvider.GetUtcNow();

        var snapshot = cart.Items
            .Select(item => (item.ProductId, item.ProductNameSnapshot, item.UnitPriceSnapshot, item.Currency, item.Quantity))
            .ToList();

        var purchase = Purchase.Create(cart.Id, snapshot, now);

        // Single SaveChangesAsync call — EF Core wraps it in one implicit transaction, so the
        // Purchase insert and the Cart-clearing update either both commit or both roll back.
        _dbContext.Purchases.Add(purchase);
        cart.ClearAfterPurchase(now);

        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Recorded Purchase {PurchaseId} from Cart {CartId} with {ItemCount} item line(s), total {Total} {Currency}",
            purchase.Id,
            cartId,
            purchase.Items.Count,
            purchase.Total,
            purchase.Currency);

        return (purchase, cart);
    }

    public async Task<IReadOnlyList<Purchase>> GetPurchaseHistoryAsync(CancellationToken cancellationToken)
    {
        return await _dbContext.Purchases
            .AsNoTracking()
            .Include(p => p.Items)
            .OrderByDescending(p => p.PurchasedAtUtc)
            .ThenByDescending(p => p.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task<Purchase> GetPurchaseAsync(Guid purchaseId, CancellationToken cancellationToken)
    {
        var purchase = await _dbContext.Purchases
            .AsNoTracking()
            .Include(p => p.Items)
            .FirstOrDefaultAsync(p => p.Id == purchaseId, cancellationToken);

        return purchase ?? throw new KeyNotFoundException($"Purchase '{purchaseId}' was not found.");
    }
}
