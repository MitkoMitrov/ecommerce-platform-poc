using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Commerce.Api.Features.Carts;

namespace Commerce.IntegrationTests;

[Collection(CommerceApiCollection.Name)]
public sealed class CartEndpointsTests
{
    private readonly HttpClient _client;

    public CartEndpointsTests(CommerceApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task CreateCart_ReturnsCreatedWithEmptyCart()
    {
        var response = await _client.PostAsync("/api/carts", content: null);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(response.Headers.Location);
        Assert.Contains("/api/carts/", response.Headers.Location!.ToString());

        var cart = await response.Content.ReadFromJsonAsync<CartResponse>();

        Assert.NotNull(cart);
        Assert.Null(cart.Currency);
        Assert.Equal(0m, cart.Subtotal);
        Assert.Empty(cart.Items);
    }

    [Fact]
    public async Task CreateCart_CanBeRetrievedThroughFreshRequest()
    {
        var createResponse = await _client.PostAsync("/api/carts", content: null);
        var created = await createResponse.Content.ReadFromJsonAsync<CartResponse>();
        Assert.NotNull(created);

        var getResponse = await _client.GetAsync($"/api/carts/{created.Id}");

        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

        var fetched = await getResponse.Content.ReadFromJsonAsync<CartResponse>();

        Assert.NotNull(fetched);
        Assert.Equal(created.Id, fetched.Id);
        Assert.Null(fetched.Currency);
        Assert.Equal(0m, fetched.Subtotal);
        Assert.Empty(fetched.Items);
    }

    [Fact]
    public async Task GetCart_MissingCart_ReturnsNotFoundProblemDetails()
    {
        var response = await _client.GetAsync($"/api/carts/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task GetCart_EmptyGuid_ReturnsBadRequest()
    {
        var response = await _client.GetAsync($"/api/carts/{Guid.Empty}");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task AddItem_UsesAuthoritativeProductSnapshot()
    {
        var cartId = await CreateCartAsync();

        var response = await AddItemAsync(cartId, TestProductIds.WirelessMouse, quantity: 2);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var cart = await response.Content.ReadFromJsonAsync<CartResponse>();
        Assert.NotNull(cart);

        var item = Assert.Single(cart.Items);
        Assert.Equal(TestProductIds.WirelessMouse, item.ProductId);
        Assert.Equal("Wireless Mouse", item.ProductName);
        Assert.Equal(29.99m, item.UnitPrice);
        Assert.Equal("EUR", item.Currency);
        Assert.Equal(2, item.Quantity);
        Assert.Equal(59.98m, item.LineTotal);
        Assert.Equal("EUR", cart.Currency);
        Assert.Equal(59.98m, cart.Subtotal);
    }

    [Fact]
    public async Task AddItem_SameProductTwice_MergesQuantityIntoSingleLine()
    {
        var cartId = await CreateCartAsync();

        await AddItemAsync(cartId, TestProductIds.WirelessMouse, quantity: 2);
        var response = await AddItemAsync(cartId, TestProductIds.WirelessMouse, quantity: 1);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var cart = await response.Content.ReadFromJsonAsync<CartResponse>();
        Assert.NotNull(cart);

        var item = Assert.Single(cart.Items);
        Assert.Equal(3, item.Quantity);
        Assert.Equal(89.97m, item.LineTotal);
        Assert.Equal(89.97m, cart.Subtotal);
    }

    [Fact]
    public async Task AddItem_ResultingQuantityAboveMax_ReturnsBadRequestAndDoesNotMutateState()
    {
        var cartId = await CreateCartAsync();

        await AddItemAsync(cartId, TestProductIds.WirelessMouse, quantity: 95);
        var response = await AddItemAsync(cartId, TestProductIds.WirelessMouse, quantity: 10);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var getResponse = await _client.GetAsync($"/api/carts/{cartId}");
        var cart = await getResponse.Content.ReadFromJsonAsync<CartResponse>();

        Assert.NotNull(cart);
        var item = Assert.Single(cart.Items);
        Assert.Equal(95, item.Quantity);
    }

    [Fact]
    public async Task AddItem_QuantityZero_ReturnsBadRequest()
    {
        var cartId = await CreateCartAsync();

        var response = await AddItemAsync(cartId, TestProductIds.WirelessMouse, quantity: 0);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task AddItem_QuantityAboveMax_ReturnsBadRequest()
    {
        var cartId = await CreateCartAsync();

        var response = await AddItemAsync(cartId, TestProductIds.WirelessMouse, quantity: 100);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task AddItem_MissingProduct_ReturnsNotFound()
    {
        var cartId = await CreateCartAsync();

        var response = await AddItemAsync(cartId, Guid.NewGuid(), quantity: 1);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task AddItem_InactiveProduct_ReturnsSamePublicNotFoundAsMissingProduct()
    {
        var cartId = await CreateCartAsync();

        var response = await AddItemAsync(cartId, TestProductIds.LegacyAdapter, quantity: 1);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task UpdateQuantity_UpdatesQuantityLineTotalSubtotalAndUpdatedAtUtc()
    {
        var cartId = await CreateCartAsync();
        var addResponse = await AddItemAsync(cartId, TestProductIds.WirelessMouse, quantity: 2);
        var afterAdd = await addResponse.Content.ReadFromJsonAsync<CartResponse>();
        Assert.NotNull(afterAdd);

        var response = await _client.PutAsJsonAsync(
            $"/api/carts/{cartId}/items/{TestProductIds.WirelessMouse}",
            new UpdateCartItemRequest(5));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var cart = await response.Content.ReadFromJsonAsync<CartResponse>();
        Assert.NotNull(cart);

        var item = Assert.Single(cart.Items);
        Assert.Equal(5, item.Quantity);
        Assert.Equal(149.95m, item.LineTotal);
        Assert.Equal(149.95m, cart.Subtotal);
        Assert.True(cart.UpdatedAtUtc >= afterAdd.UpdatedAtUtc);
    }

    [Fact]
    public async Task UpdateQuantity_DoesNotRefreshProductSnapshot()
    {
        var cartId = await CreateCartAsync();
        await AddItemAsync(cartId, TestProductIds.WirelessMouse, quantity: 1);

        var response = await _client.PutAsJsonAsync(
            $"/api/carts/{cartId}/items/{TestProductIds.WirelessMouse}",
            new UpdateCartItemRequest(3));

        var cart = await response.Content.ReadFromJsonAsync<CartResponse>();
        Assert.NotNull(cart);

        var item = Assert.Single(cart.Items);
        Assert.Equal("Wireless Mouse", item.ProductName);
        Assert.Equal(29.99m, item.UnitPrice);
    }

    [Fact]
    public async Task UpdateQuantity_MissingCartItem_ReturnsNotFound()
    {
        var cartId = await CreateCartAsync();

        var response = await _client.PutAsJsonAsync(
            $"/api/carts/{cartId}/items/{TestProductIds.WirelessMouse}",
            new UpdateCartItemRequest(3));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task RemoveItem_ReturnsNoContent()
    {
        var cartId = await CreateCartAsync();
        await AddItemAsync(cartId, TestProductIds.WirelessMouse, quantity: 1);

        var response = await _client.DeleteAsync($"/api/carts/{cartId}/items/{TestProductIds.WirelessMouse}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Empty(body);
    }

    [Fact]
    public async Task RemoveItem_ItemAbsentOnSubsequentGetRequest()
    {
        var cartId = await CreateCartAsync();
        await AddItemAsync(cartId, TestProductIds.WirelessMouse, quantity: 1);
        await _client.DeleteAsync($"/api/carts/{cartId}/items/{TestProductIds.WirelessMouse}");

        var getResponse = await _client.GetAsync($"/api/carts/{cartId}");
        var cart = await getResponse.Content.ReadFromJsonAsync<CartResponse>();

        Assert.NotNull(cart);
        Assert.Empty(cart.Items);
        Assert.Equal(0m, cart.Subtotal);
        Assert.Null(cart.Currency);
    }

    [Fact]
    public async Task RemoveItem_SecondRemovalOfSameItem_ReturnsNotFound()
    {
        var cartId = await CreateCartAsync();
        await AddItemAsync(cartId, TestProductIds.WirelessMouse, quantity: 1);
        await _client.DeleteAsync($"/api/carts/{cartId}/items/{TestProductIds.WirelessMouse}");

        var response = await _client.DeleteAsync($"/api/carts/{cartId}/items/{TestProductIds.WirelessMouse}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task ProblemDetailsResponse_ContainsTraceIdExtension()
    {
        var response = await _client.GetAsync($"/api/carts/{Guid.NewGuid()}");

        var payload = await response.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(payload);
        var root = document.RootElement;

        Assert.Equal(404, root.GetProperty("status").GetInt32());
        Assert.False(string.IsNullOrWhiteSpace(root.GetProperty("title").GetString()));
        Assert.True(root.TryGetProperty("traceId", out var traceId));
        Assert.False(string.IsNullOrWhiteSpace(traceId.GetString()));
    }

    [Fact]
    public async Task AddItem_MissingRequestBody_ReturnsProblemDetails()
    {
        var cartId = await CreateCartAsync();

        var response = await _client.PostAsync($"/api/carts/{cartId}/items", content: null);

        await AssertProblemDetailsAsync(response, expectedStatus: 400);
    }

    [Fact]
    public async Task AddItem_WithMalformedJson_ReturnsProblemDetails()
    {
        var cartId = await CreateCartAsync();

        var malformedContent = new StringContent("{ this is not valid json", Encoding.UTF8, "application/json");
        var response = await _client.PostAsync($"/api/carts/{cartId}/items", malformedContent);

        await AssertProblemDetailsAsync(response, expectedStatus: 400);
    }

    private async Task<Guid> CreateCartAsync()
    {
        var response = await _client.PostAsync("/api/carts", content: null);
        var cart = await response.Content.ReadFromJsonAsync<CartResponse>();
        Assert.NotNull(cart);
        return cart.Id;
    }

    private Task<HttpResponseMessage> AddItemAsync(Guid cartId, Guid productId, int quantity)
    {
        return _client.PostAsJsonAsync($"/api/carts/{cartId}/items", new AddCartItemRequest(productId, quantity));
    }

    private static async Task AssertProblemDetailsAsync(HttpResponseMessage response, int expectedStatus)
    {
        Assert.Equal(expectedStatus, (int)response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);

        var payload = await response.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(payload);
        var root = document.RootElement;

        Assert.Equal(expectedStatus, root.GetProperty("status").GetInt32());
        Assert.False(string.IsNullOrWhiteSpace(root.GetProperty("title").GetString()));
        Assert.True(root.TryGetProperty("traceId", out var traceId));
        Assert.False(string.IsNullOrWhiteSpace(traceId.GetString()));
    }
}
