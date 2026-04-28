using CarrierRatesQueryV2.Tests.Infrastructure;
using Shouldly;
using System.Net;
using Xunit;

namespace CarrierRatesQueryV2.Tests.Features.CarrierEndpoints.Delete.Integration;

[Collection("IntegrationTests")]
public class DeleteCarrierEndpointIntegrationTests : IntegrationTestBase
{
    public DeleteCarrierEndpointIntegrationTests(TestWebApplicationFactory factory) : base(factory) { }

    [Fact]
    public async Task DeleteCarrierEndpoint_WithExistingEndpoint_ShouldReturn204()
    {
        var carrier = await AddCarrierAsync("Carrier Endpoint Delete");
        var endpoint = await AddCarrierEndpointAsync(carrier.Id);

        var response = await Client.DeleteAsync($"/api/v1/carriers/{carrier.Id}/endpoints/{endpoint.Id}");

        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task DeleteCarrierEndpoint_WithNonExistentId_ShouldReturn404()
    {
        var carrier = await AddCarrierAsync("Carrier Endpoint Delete 2");

        var response = await Client.DeleteAsync($"/api/v1/carriers/{carrier.Id}/endpoints/{Guid.NewGuid()}");

        await ShouldHaveStatusAsync(response, HttpStatusCode.NotFound);
    }
}
