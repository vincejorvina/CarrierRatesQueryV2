using CarrierRatesQueryV2.Tests.Infrastructure;
using Shouldly;
using System.Net;
using System.Text.Json;
using Xunit;

namespace CarrierRatesQueryV2.Tests.Features.Shipments.GetAll.Integration;

[Collection("IntegrationTests")]
public class GetAllShipmentsIntegrationTests : IntegrationTestBase
{
    public GetAllShipmentsIntegrationTests(TestWebApplicationFactory factory) : base(factory) { }

    [Fact]
    public async Task GetAllShipments_WithSeededData_ShouldReturn200()
    {
        var response = await Client.GetAsync("/api/v1/shipments");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var root = await ReadJsonAsync(response);
        root.ValueKind.ShouldBe(JsonValueKind.Array);
    }
}
