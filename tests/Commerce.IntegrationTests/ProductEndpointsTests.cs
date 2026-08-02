using System.Net;
using System.Net.Http.Json;
using Commerce.Api.Features.Products;

namespace Commerce.IntegrationTests;

[Collection(CommerceApiCollection.Name)]
public sealed class ProductEndpointsTests
{
    private readonly HttpClient _client;

    public ProductEndpointsTests(CommerceApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetProducts_ReturnsActiveProductsInDeterministicOrder()
    {
        var response = await _client.GetAsync("/api/products");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var products = await response.Content.ReadFromJsonAsync<List<ProductResponse>>();

        Assert.NotNull(products);
        Assert.Equal(3, products.Count);

        Assert.Equal("Mechanical Keyboard", products[0].Name);
        Assert.Equal("USB-C Hub", products[1].Name);
        Assert.Equal("Wireless Mouse", products[2].Name);

        Assert.DoesNotContain(products, product => product.Name == "Legacy Adapter");
    }

    [Fact]
    public async Task GetProducts_ReturnsAuthoritativeUnitPriceAndCurrency()
    {
        var response = await _client.GetAsync("/api/products");
        var products = await response.Content.ReadFromJsonAsync<List<ProductResponse>>();

        Assert.NotNull(products);
        var mouse = products.Single(product => product.Id == TestProductIds.WirelessMouse);

        Assert.Equal(29.99m, mouse.UnitPrice);
        Assert.Equal("EUR", mouse.Currency);
    }
}
