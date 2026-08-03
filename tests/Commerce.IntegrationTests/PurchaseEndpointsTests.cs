using System.Net;
using System.Net.Http.Json;
using Commerce.Api.Features.Carts;
using Commerce.Api.Features.Purchases;

namespace Commerce.IntegrationTests;

[Collection(CommerceApiCollection.Name)]
public sealed class PurchaseEndpointsTests
{
    private readonly HttpClient _client;

    public PurchaseEndpointsTests(CommerceApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task PurchaseCart_WithNonEmptyCart_ReturnsCreated()
    {
        var cartId = await CreateCartAsync();
        await AddItemAsync(cartId, TestProductIds.WirelessMouse, quantity: 2);

        var response = await PurchaseCartAsync(cartId);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(response.Headers.Location);
        Assert.Contains("/api/purchases/", response.Headers.Location!.ToString());
    }

    [Fact]
    public async Task PurchaseCart_ResponseContainsPurchaseAndEmptyCartWithSameId()
    {
        var cartId = await CreateCartAsync();
        await AddItemAsync(cartId, TestProductIds.WirelessMouse, quantity: 2);

        var response = await PurchaseCartAsync(cartId);
        var payload = await response.Content.ReadFromJsonAsync<PurchaseCartResponse>();

        Assert.NotNull(payload);
        Assert.Equal(cartId, payload.Purchase.CartId);
        Assert.Equal(cartId, payload.Cart.Id);
        Assert.Empty(payload.Cart.Items);
        Assert.Equal(0m, payload.Cart.Subtotal);
        Assert.Null(payload.Cart.Currency);
    }

    [Fact]
    public async Task PurchaseCart_UsesAuthoritativeServerSidePrices()
    {
        var cartId = await CreateCartAsync();
        await AddItemAsync(cartId, TestProductIds.WirelessMouse, quantity: 2);
        await AddItemAsync(cartId, TestProductIds.UsbCHub, quantity: 1);

        var response = await PurchaseCartAsync(cartId);
        var payload = await response.Content.ReadFromJsonAsync<PurchaseCartResponse>();

        Assert.NotNull(payload);
        Assert.Equal(2, payload.Purchase.Items.Count);

        var mouse = payload.Purchase.Items.Single(item => item.ProductId == TestProductIds.WirelessMouse);
        Assert.Equal("Wireless Mouse", mouse.ProductName);
        Assert.Equal(29.99m, mouse.UnitPrice);
        Assert.Equal("EUR", mouse.Currency);
        Assert.Equal(2, mouse.Quantity);
        Assert.Equal(59.98m, mouse.LineTotal);

        var hub = payload.Purchase.Items.Single(item => item.ProductId == TestProductIds.UsbCHub);
        Assert.Equal("USB-C Hub", hub.ProductName);
        Assert.Equal(49.50m, hub.UnitPrice);
        Assert.Equal(1, hub.Quantity);
        Assert.Equal(49.50m, hub.LineTotal);

        Assert.Equal("EUR", payload.Purchase.Currency);
        Assert.Equal(109.48m, payload.Purchase.Total);
    }

    [Fact]
    public async Task PurchaseCart_SameCartCanBeRetrievedAfterwardAndIsEmpty()
    {
        var cartId = await CreateCartAsync();
        await AddItemAsync(cartId, TestProductIds.WirelessMouse, quantity: 1);

        await PurchaseCartAsync(cartId);

        var getResponse = await _client.GetAsync($"/api/carts/{cartId}");
        var cart = await getResponse.Content.ReadFromJsonAsync<CartResponse>();

        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
        Assert.NotNull(cart);
        Assert.Equal(cartId, cart.Id);
        Assert.Empty(cart.Items);
        Assert.Equal(0m, cart.Subtotal);
        Assert.Null(cart.Currency);
    }

    [Fact]
    public async Task PurchaseCart_EmptyCart_ReturnsConflictProblemDetails()
    {
        var cartId = await CreateCartAsync();

        var response = await PurchaseCartAsync(cartId);

        await AssertProblemDetailsAsync(response, expectedStatus: 409);
    }

    [Fact]
    public async Task PurchaseCart_ConcurrentRequestsForSameCart_ExactlyOneSucceeds()
    {
        var cartId = await CreateCartAsync();
        await AddItemAsync(cartId, TestProductIds.WirelessMouse, quantity: 1);

        var firstRequest = PurchaseCartAsync(cartId);
        var secondRequest = PurchaseCartAsync(cartId);
        var responses = await Task.WhenAll(firstRequest, secondRequest);

        var statusCodes = responses.Select(r => r.StatusCode).OrderBy(status => (int)status).ToList();
        Assert.Equal(new[] { HttpStatusCode.Created, HttpStatusCode.Conflict }, statusCodes);

        var loser = responses.Single(r => r.StatusCode == HttpStatusCode.Conflict);
        await AssertProblemDetailsAsync(loser, expectedStatus: 409);

        var winner = responses.Single(r => r.StatusCode == HttpStatusCode.Created);
        var winnerPayload = await winner.Content.ReadFromJsonAsync<PurchaseCartResponse>();
        Assert.NotNull(winnerPayload);
        Assert.Empty(winnerPayload.Cart.Items);

        var cartResponse = await _client.GetAsync($"/api/carts/{cartId}");
        var cart = await cartResponse.Content.ReadFromJsonAsync<CartResponse>();
        Assert.NotNull(cart);
        Assert.Empty(cart.Items);
        Assert.Equal(0m, cart.Subtotal);

        var historyResponse = await _client.GetAsync("/api/purchases");
        var history = await historyResponse.Content.ReadFromJsonAsync<List<PurchaseResponse>>();
        Assert.NotNull(history);
        var purchasesForCart = history.Where(purchase => purchase.CartId == cartId).ToList();
        var purchase = Assert.Single(purchasesForCart);
        var item = Assert.Single(purchase.Items);
        Assert.Equal(TestProductIds.WirelessMouse, item.ProductId);
        Assert.Equal(1, item.Quantity);
    }

    [Fact]
    public async Task PurchaseCart_MissingCart_ReturnsNotFound()
    {
        var response = await PurchaseCartAsync(Guid.NewGuid());

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task PurchaseCart_EmptyGuid_ReturnsBadRequest()
    {
        var response = await PurchaseCartAsync(Guid.Empty);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task PurchaseCart_FailedEmptyCartPurchase_DoesNotCreateHistoryRecord()
    {
        var cartId = await CreateCartAsync();

        var response = await PurchaseCartAsync(cartId);
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);

        var historyResponse = await _client.GetAsync("/api/purchases");
        var history = await historyResponse.Content.ReadFromJsonAsync<List<PurchaseResponse>>();

        Assert.NotNull(history);
        Assert.DoesNotContain(history, purchase => purchase.CartId == cartId);
    }

    [Fact]
    public async Task GetPurchaseHistory_ContainsTheCreatedPurchase()
    {
        var cartId = await CreateCartAsync();
        await AddItemAsync(cartId, TestProductIds.WirelessMouse, quantity: 1);
        var purchaseResponse = await PurchaseCartAsync(cartId);
        var created = await purchaseResponse.Content.ReadFromJsonAsync<PurchaseCartResponse>();
        Assert.NotNull(created);

        var historyResponse = await _client.GetAsync("/api/purchases");
        var history = await historyResponse.Content.ReadFromJsonAsync<List<PurchaseResponse>>();

        Assert.Equal(HttpStatusCode.OK, historyResponse.StatusCode);
        Assert.NotNull(history);
        var found = Assert.Single(history, purchase => purchase.Id == created.Purchase.Id);
        Assert.Equal(cartId, found.CartId);
        Assert.Equal("EUR", found.Currency);
        Assert.Equal(29.99m, found.Total);
        var item = Assert.Single(found.Items);
        Assert.Equal("Wireless Mouse", item.ProductName);
    }

    [Fact]
    public async Task GetPurchaseHistory_OrdersNewestFirst()
    {
        var olderCartId = await CreateCartAsync();
        await AddItemAsync(olderCartId, TestProductIds.WirelessMouse, quantity: 1);
        var olderResponse = await PurchaseCartAsync(olderCartId);
        var older = await olderResponse.Content.ReadFromJsonAsync<PurchaseCartResponse>();
        Assert.NotNull(older);

        var newerCartId = await CreateCartAsync();
        await AddItemAsync(newerCartId, TestProductIds.UsbCHub, quantity: 1);
        var newerResponse = await PurchaseCartAsync(newerCartId);
        var newer = await newerResponse.Content.ReadFromJsonAsync<PurchaseCartResponse>();
        Assert.NotNull(newer);

        var historyResponse = await _client.GetAsync("/api/purchases");
        var history = await historyResponse.Content.ReadFromJsonAsync<List<PurchaseResponse>>();

        Assert.NotNull(history);
        var newerIndex = history.FindIndex(purchase => purchase.Id == newer.Purchase.Id);
        var olderIndex = history.FindIndex(purchase => purchase.Id == older.Purchase.Id);

        Assert.True(newerIndex >= 0 && olderIndex >= 0);
        Assert.True(newerIndex < olderIndex);
    }

    [Fact]
    public async Task GetPurchase_ReturnsThePersistedSnapshot()
    {
        var cartId = await CreateCartAsync();
        await AddItemAsync(cartId, TestProductIds.MechanicalKeyboard, quantity: 3);
        var purchaseResponse = await PurchaseCartAsync(cartId);
        var created = await purchaseResponse.Content.ReadFromJsonAsync<PurchaseCartResponse>();
        Assert.NotNull(created);

        var response = await _client.GetAsync($"/api/purchases/{created.Purchase.Id}");
        var purchase = await response.Content.ReadFromJsonAsync<PurchaseResponse>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(purchase);
        Assert.Equal(created.Purchase.Id, purchase.Id);
        Assert.Equal(cartId, purchase.CartId);
        Assert.Equal("EUR", purchase.Currency);
        Assert.Equal(269.97m, purchase.Total);
        var item = Assert.Single(purchase.Items);
        Assert.Equal("Mechanical Keyboard", item.ProductName);
        Assert.Equal(89.99m, item.UnitPrice);
        Assert.Equal(3, item.Quantity);
        Assert.Equal(269.97m, item.LineTotal);
    }

    [Fact]
    public async Task GetPurchase_MissingPurchase_ReturnsNotFound()
    {
        var response = await _client.GetAsync($"/api/purchases/{Guid.NewGuid()}");

        await AssertProblemDetailsAsync(response, expectedStatus: 404);
    }

    [Fact]
    public async Task GetPurchase_EmptyGuid_ReturnsBadRequest()
    {
        var response = await _client.GetAsync($"/api/purchases/{Guid.Empty}");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
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

    private Task<HttpResponseMessage> PurchaseCartAsync(Guid cartId)
    {
        return _client.PostAsync($"/api/carts/{cartId}/purchase", content: null);
    }

    private static async Task AssertProblemDetailsAsync(HttpResponseMessage response, int expectedStatus)
    {
        Assert.Equal(expectedStatus, (int)response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);

        var payload = await response.Content.ReadAsStringAsync();
        using var document = System.Text.Json.JsonDocument.Parse(payload);
        var root = document.RootElement;

        Assert.Equal(expectedStatus, root.GetProperty("status").GetInt32());
        Assert.False(string.IsNullOrWhiteSpace(root.GetProperty("title").GetString()));
        Assert.True(root.TryGetProperty("traceId", out var traceId));
        Assert.False(string.IsNullOrWhiteSpace(traceId.GetString()));
    }
}
