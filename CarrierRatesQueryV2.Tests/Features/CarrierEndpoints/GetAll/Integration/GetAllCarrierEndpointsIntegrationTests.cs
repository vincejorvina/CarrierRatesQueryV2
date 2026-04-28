using CarrierRatesQueryV2.Tests.Infrastructure;
using Shouldly;
using System.Net;
using Xunit;

namespace CarrierRatesQueryV2.Tests.Features.CarrierEndpoints.GetAll.Integration;

[Collection("IntegrationTests")]
public class GetAllCarrierEndpointsIntegrationTests : IntegrationTestBase
{
    public GetAllCarrierEndpointsIntegrationTests(TestWebApplicationFactory factory) : base(factory) { }

    [Fact]
    public async Task GetAllCarrierEndpoints_WithSeededCarrier_ShouldReturn200()
    {
        var carrier = await GetCarrierByNameAsync("FedEx");

        var response = await Client.GetAsync($"/api/v1/carriers/{carrier.Id}/endpoints");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var root = await ReadJsonAsync(response);
        root.GetArrayLength().ShouldBeGreaterThanOrEqualTo(1);
    }
}
