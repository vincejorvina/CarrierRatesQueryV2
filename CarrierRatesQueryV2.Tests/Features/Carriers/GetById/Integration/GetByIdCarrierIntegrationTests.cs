using CarrierRatesQueryV2.Tests.Infrastructure;
using Shouldly;
using System.Net;
using Xunit;

namespace CarrierRatesQueryV2.Tests.Features.Carriers.GetById.Integration;

[Collection("IntegrationTests")]
public class GetByIdCarrierIntegrationTests : IntegrationTestBase
{
    public GetByIdCarrierIntegrationTests(TestWebApplicationFactory factory) : base(factory) { }

    [Fact]
    public async Task GetCarrierById_WithSeededCarrier_ShouldReturn200()
    {
        var carrier = await GetCarrierByNameAsync("FedEx");

        var response = await Client.GetAsync($"/api/v1/carriers/{carrier.Id}");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var root = await ReadJsonAsync(response);
        GetGuid(root, "id").ShouldBe(carrier.Id);
    }

    [Fact]
    public async Task GetCarrierById_WithNonExistentId_ShouldReturn404()
    {
        var response = await Client.GetAsync($"/api/v1/carriers/{Guid.NewGuid()}");

        await ShouldHaveStatusAsync(response, HttpStatusCode.NotFound);
    }
}
