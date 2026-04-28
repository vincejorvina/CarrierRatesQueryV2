using CarrierRatesQueryV2.Tests.Infrastructure;
using Shouldly;
using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace CarrierRatesQueryV2.Tests.Features.Shipments.Update.Integration;

[Collection("IntegrationTests")]
public class UpdateShipmentIntegrationTests : IntegrationTestBase
{
    public UpdateShipmentIntegrationTests(TestWebApplicationFactory factory) : base(factory) { }

    [Fact]
    public async Task UpdateShipment_WithValidStatus_ShouldReturn200()
    {
        var carrier = await GetCarrierByNameAsync("FedEx");
        var shipment = await AddShipmentAsync(carrier.Id);

        var response = await Client.PatchAsJsonAsync($"/api/v1/shipments/{shipment.Id}", new { id = shipment.Id, status = "Completed" });

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var root = await ReadJsonAsync(response);
        GetString(root, "status").ShouldBe("Completed");
    }

    [Fact]
    public async Task UpdateShipment_WithNonExistentId_ShouldReturn404()
    {
        var response = await Client.PatchAsJsonAsync($"/api/v1/shipments/{Guid.NewGuid()}", new { id = Guid.NewGuid(), status = "Completed" });

        await ShouldHaveStatusAsync(response, HttpStatusCode.NotFound);
    }
}
