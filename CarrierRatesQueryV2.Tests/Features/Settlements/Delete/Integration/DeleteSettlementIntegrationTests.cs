using CarrierRatesQueryV2.Tests.Infrastructure;
using Shouldly;
using System.Net;
using Xunit;

namespace CarrierRatesQueryV2.Tests.Features.Settlements.Delete.Integration;

[Collection("IntegrationTests")]
public class DeleteSettlementIntegrationTests : IntegrationTestBase
{
    public DeleteSettlementIntegrationTests(TestWebApplicationFactory factory) : base(factory) { }

    [Fact]
    public async Task DeleteSettlement_WithExistingSettlement_ShouldReturn204()
    {
        var carrier = await GetCarrierByNameAsync("DHL");
        var settlement = await AddSettlementAsync(carrier.Id);

        var response = await Client.DeleteAsync($"/api/v1/settlements/{settlement.Id}");

        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
    }
}
