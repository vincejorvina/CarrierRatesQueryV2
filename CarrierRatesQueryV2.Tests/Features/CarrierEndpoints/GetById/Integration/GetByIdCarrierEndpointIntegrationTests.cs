using CarrierRatesQueryV2.Tests.Infrastructure;
using Shouldly;
using System.Net;
using Xunit;

namespace CarrierRatesQueryV2.Tests.Features.CarrierEndpoints.GetById.Integration;

[Collection("IntegrationTests")]
public class GetByIdCarrierEndpointIntegrationTests : IntegrationTestBase
{
    public GetByIdCarrierEndpointIntegrationTests(TestWebApplicationFactory factory) : base(factory) { }

    [Fact]
    public async Task GetCarrierEndpointById_WithSeededEndpoint_ShouldReturn200()
    {
        var carrier = await GetCarrierByNameAsync("FedEx");
        var endpoint = carrier.Endpoints.Single();

        var response = await Client.GetAsync($"/api/v1/carriers/{carrier.Id}/endpoints/{endpoint.Id}");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var root = await ReadJsonAsync(response);
        GetGuid(root, "id").ShouldBe(endpoint.Id);
    }
}
