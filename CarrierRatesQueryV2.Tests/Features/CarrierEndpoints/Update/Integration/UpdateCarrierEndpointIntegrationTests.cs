using CarrierRatesQueryV2.Tests.Infrastructure;
using Shouldly;
using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace CarrierRatesQueryV2.Tests.Features.CarrierEndpoints.Update.Integration;

[Collection("IntegrationTests")]
public class UpdateCarrierEndpointIntegrationTests : IntegrationTestBase
{
    public UpdateCarrierEndpointIntegrationTests(TestWebApplicationFactory factory) : base(factory) { }

    [Fact]
    public async Task UpdateCarrierEndpoint_WithValidRequest_ShouldReturn200()
    {
        var carrier = await AddCarrierAsync("Carrier Endpoint Update");
        var endpoint = await AddCarrierEndpointAsync(carrier.Id);

        var response = await Client.PutAsJsonAsync($"/api/v1/carriers/{carrier.Id}/endpoints/{endpoint.Id}", new
        {
            carrierId = carrier.Id,
            endpointId = endpoint.Id,
            operation = "Rates",
            endpoint = "https://example.test/v2/rates"
        });

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var root = await ReadJsonAsync(response);
        GetString(root, "endpoint").ShouldBe("https://example.test/v2/rates");
    }

    [Fact]
    public async Task UpdateCarrierEndpoint_WithNonExistentId_ShouldReturn404()
    {
        var carrier = await AddCarrierAsync("Carrier Endpoint Update 2");

        var response = await Client.PutAsJsonAsync($"/api/v1/carriers/{carrier.Id}/endpoints/{Guid.NewGuid()}", new
        {
            carrierId = carrier.Id,
            endpointId = Guid.NewGuid(),
            operation = "Rates",
            endpoint = "https://example.test/v2/rates"
        });

        await ShouldHaveStatusAsync(response, HttpStatusCode.NotFound);
    }
}
