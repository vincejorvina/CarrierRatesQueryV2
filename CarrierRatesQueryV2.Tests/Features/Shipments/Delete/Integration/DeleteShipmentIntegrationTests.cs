using CarrierRatesQueryV2.Tests.Infrastructure;
using Shouldly;
using System.Net;
using Xunit;

namespace CarrierRatesQueryV2.Tests.Features.Shipments.Delete.Integration;

[Collection("IntegrationTests")]
public class DeleteShipmentIntegrationTests : IntegrationTestBase
{
    public DeleteShipmentIntegrationTests(TestWebApplicationFactory factory) : base(factory) { }

    [Fact]
    public async Task DeleteShipment_WithExistingShipment_ShouldReturn204()
    {
        var carrier = await GetCarrierByNameAsync("FedEx");
        var shipment = await AddShipmentAsync(carrier.Id);

        var response = await Client.DeleteAsync($"/api/v1/shipments/{shipment.Id}");

        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
    }
}
