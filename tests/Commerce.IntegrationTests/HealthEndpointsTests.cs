using System.Net;

namespace Commerce.IntegrationTests;

[Collection(CommerceApiCollection.Name)]
public sealed class HealthEndpointsTests
{
    private readonly HttpClient _client;

    public HealthEndpointsTests(CommerceApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Liveness_ReturnsOk()
    {
        var response = await _client.GetAsync("/health/live");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Readiness_ReturnsOkWhenPostgresIsReachable()
    {
        var response = await _client.GetAsync("/health/ready");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
