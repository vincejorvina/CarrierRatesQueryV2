using CarrierRatesQueryV2.Tests.Infrastructure;
using Shouldly;
using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace CarrierRatesQueryV2.Tests.Features.Rates.QueryByCarrierId.Integration;

[Collection("IntegrationTests")]
public class QueryByCarrierIdIntegrationTests : IntegrationTestBase
{
    public QueryByCarrierIdIntegrationTests(TestWebApplicationFactory factory) : base(factory) { }

    [Fact]
    public async Task QueryRatesByCarrierId_WithValidRequest_ShouldReturn200()
    {
        var carrier = await GetCarrierByNameAsync("FedEx");

        var response = await Client.PostAsJsonAsync($"/api/v1/rates/carrier/{carrier.Id}", CreateRateRequest(new { carrierId = carrier.Id }));

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var root = await ReadJsonAsync(response);
        GetString(root, "carrier").ShouldBe("FedEx");
    }

    [Fact]
    public async Task QueryRatesByCarrierId_WithNonExistentId_ShouldReturn404()
    {
        var response = await Client.PostAsJsonAsync($"/api/v1/rates/carrier/{Guid.NewGuid()}", CreateRateRequest(new { carrierId = Guid.NewGuid() }));

        await ShouldHaveStatusAsync(response, HttpStatusCode.NotFound);
    }
}
