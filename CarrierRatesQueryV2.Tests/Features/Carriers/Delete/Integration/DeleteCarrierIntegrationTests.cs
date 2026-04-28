using CarrierRatesQueryV2.Tests.Infrastructure;
using Shouldly;
using System.Net;
using Xunit;

namespace CarrierRatesQueryV2.Tests.Features.Carriers.Delete.Integration;

[Collection("IntegrationTests")]
public class DeleteCarrierIntegrationTests : IntegrationTestBase
{
    public DeleteCarrierIntegrationTests(TestWebApplicationFactory factory) : base(factory) { }

    [Fact]
    public async Task DeleteCarrier_WithExistingCarrier_ShouldReturn204()
    {
        var carrier = await AddCarrierAsync("Carrier To Delete");

        var response = await Client.DeleteAsync($"/api/v1/carriers/{carrier.Id}");

        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
    }
}
