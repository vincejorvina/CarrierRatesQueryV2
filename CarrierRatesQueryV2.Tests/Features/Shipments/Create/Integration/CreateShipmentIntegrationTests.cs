using CarrierRatesQueryV2.Tests.Infrastructure;
using Shouldly;
using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace CarrierRatesQueryV2.Tests.Features.Shipments.Create.Integration;

[Collection("IntegrationTests")]
public class CreateShipmentIntegrationTests : IntegrationTestBase
{
    public CreateShipmentIntegrationTests(TestWebApplicationFactory factory) : base(factory) { }

    [Fact]
    public async Task CreateShipment_WithValidCarrier_ShouldReturn201()
    {
        var carrier = await GetCarrierByNameAsync("FedEx");

        var response = await Client.PostAsJsonAsync($"/api/v1/carriers/{carrier.Id}/shipments", new { carrierId = carrier.Id });

        await ShouldHaveStatusAsync(response, HttpStatusCode.Created);
        var root = await ReadJsonAsync(response);
        GetString(root, "status").ShouldBe("Pending");
    }
}
