using CarrierRatesQueryV2.Tests.Infrastructure;
using Shouldly;
using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace CarrierRatesQueryV2.Tests.Features.Carriers.Update.Integration;

[Collection("IntegrationTests")]
public class UpdateCarrierIntegrationTests : IntegrationTestBase
{
    public UpdateCarrierIntegrationTests(TestWebApplicationFactory factory) : base(factory) { }

    [Fact]
    public async Task UpdateCarrier_WithValidRequest_ShouldReturn204()
    {
        var carrier = await AddCarrierAsync("Carrier To Update");

        var response = await Client.PutAsJsonAsync($"/api/v1/carriers/{carrier.Id}", new { id = carrier.Id, name = "Updated Carrier", isEnabled = true });

        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
    }
}
