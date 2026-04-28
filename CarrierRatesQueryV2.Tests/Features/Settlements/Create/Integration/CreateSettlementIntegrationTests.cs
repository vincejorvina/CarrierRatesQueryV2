using CarrierRatesQueryV2.Tests.Infrastructure;
using Shouldly;
using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace CarrierRatesQueryV2.Tests.Features.Settlements.Create.Integration;

[Collection("IntegrationTests")]
public class CreateSettlementIntegrationTests : IntegrationTestBase
{
    public CreateSettlementIntegrationTests(TestWebApplicationFactory factory) : base(factory) { }

    [Fact]
    public async Task CreateSettlement_WithValidCarrier_ShouldReturn201()
    {
        var carrier = await GetCarrierByNameAsync("DHL");

        var response = await Client.PostAsJsonAsync($"/api/v1/carriers/{carrier.Id}/settlements", new { carrierId = carrier.Id });

        await ShouldHaveStatusAsync(response, HttpStatusCode.Created);
        var root = await ReadJsonAsync(response);
        GetString(root, "status").ShouldBe("Pending");
    }
}
