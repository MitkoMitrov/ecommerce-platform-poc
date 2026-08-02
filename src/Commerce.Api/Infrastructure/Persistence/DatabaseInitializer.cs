using Commerce.Api.Domain.Products;
using Microsoft.EntityFrameworkCore;

namespace Commerce.Api.Infrastructure.Persistence;

public static class DatabaseInitializer
{
    private static readonly (Guid Id, string Name, decimal UnitPrice, string Currency, bool IsActive)[] SeedProducts =
    [
        (Guid.Parse("11111111-1111-1111-1111-111111111111"), "Wireless Mouse", 29.99m, "EUR", true),
        (Guid.Parse("22222222-2222-2222-2222-222222222222"), "Mechanical Keyboard", 89.99m, "EUR", true),
        (Guid.Parse("33333333-3333-3333-3333-333333333333"), "USB-C Hub", 49.50m, "EUR", true),
        (Guid.Parse("44444444-4444-4444-4444-444444444444"), "Legacy Adapter", 19.99m, "EUR", false),
    ];

    public static async Task InitializeAsync(IServiceProvider serviceProvider, CancellationToken cancellationToken)
    {
        await using var scope = serviceProvider.CreateAsyncScope();

        var dbContext = scope.ServiceProvider.GetRequiredService<CommerceDbContext>();
        var logger = scope.ServiceProvider
            .GetRequiredService<ILoggerFactory>()
            .CreateLogger(typeof(DatabaseInitializer).FullName!);

        logger.LogInformation("Starting database migration");
        await dbContext.Database.MigrateAsync(cancellationToken);
        logger.LogInformation("Database migration completed");

        var seedIds = SeedProducts.Select(seed => seed.Id).ToArray();

        var existingIds = await dbContext.Products
            .Where(product => seedIds.Contains(product.Id))
            .Select(product => product.Id)
            .ToListAsync(cancellationToken);

        var missingProducts = SeedProducts
            .Where(seed => !existingIds.Contains(seed.Id))
            .Select(seed => Product.Create(seed.Id, seed.Name, seed.UnitPrice, seed.Currency, seed.IsActive))
            .ToList();

        if (missingProducts.Count > 0)
        {
            dbContext.Products.AddRange(missingProducts);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        logger.LogInformation(
            "Seeded {SeededProductCount} product(s); {ExistingProductCount} already present",
            missingProducts.Count,
            existingIds.Count);
    }
}
