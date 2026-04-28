using CarrierRatesQueryV2.Tests.Infrastructure;
using Shouldly;
using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace CarrierRatesQueryV2.Tests.Features.Settlements.Update.Integration;

[Collection("IntegrationTests")]
public class UpdateSettlementIntegrationTests : IntegrationTestBase
{
    public UpdateSettlementIntegrationTests(TestWebApplicationFactory factory) : base(factory) { }

    [Fact]
    public async Task UpdateSettlement_WithValidStatus_ShouldReturn200()
    {
        var carrier = await GetCarrierByNameAsync("DHL");
        var settlement = await AddSettlementAsync(carrier.Id);

        var response = await Client.PatchAsJsonAsync($"/api/v1/settlements/{settlement.Id}", new { id = settlement.Id, status = "Settled" });

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var root = await ReadJsonAsync(response);
        GetString(root, "status").ShouldBe("Settled");
    }

    [Fact]
    public async Task UpdateSettlement_WithNonExistentId_ShouldReturn404()
    {
        var response = await Client.PatchAsJsonAsync($"/api/v1/settlements/{Guid.NewGuid()}", new { id = Guid.NewGuid(), status = "Settled" });

        await ShouldHaveStatusAsync(response, HttpStatusCode.NotFound);
    }
}
