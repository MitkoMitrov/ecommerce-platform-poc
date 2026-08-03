using Commerce.Api.Features.Carts;
using Commerce.Api.Features.Products;
using Commerce.Api.Features.Purchases;
using Commerce.Api.Infrastructure.Errors;
using Commerce.Api.Infrastructure.Persistence;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

builder.Services.Configure<RouteHandlerOptions>(
    options => options.ThrowOnBadRequest = true);

var connectionString = builder.Configuration.GetConnectionString("CommerceDatabase");
if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException("Connection string 'CommerceDatabase' is required.");
}

builder.Services.AddDbContext<CommerceDbContext>(options => options.UseNpgsql(connectionString));

builder.Services.AddSingleton(TimeProvider.System);

builder.Services.AddScoped<CartService>();
builder.Services.AddScoped<PurchaseService>();

builder.Services
    .AddHealthChecks()
    .AddCheck("self", () => HealthCheckResult.Healthy(), tags: ["live"])
    .AddDbContextCheck<CommerceDbContext>("postgres", tags: ["ready"]);

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseExceptionHandler();

await DatabaseInitializer.InitializeAsync(app.Services, CancellationToken.None);

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("live"),
});

app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready"),
});

app.MapProductEndpoints();

app.MapCartEndpoints();

app.MapPurchaseEndpoints();

app.Run();

public partial class Program;
