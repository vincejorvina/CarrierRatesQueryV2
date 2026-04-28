using CarrierRatesQueryV2.Tests.Infrastructure;
using Shouldly;
using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace CarrierRatesQueryV2.Tests.Features.Carriers.Enable.Integration;

[Collection("IntegrationTests")]
public class EnableCarrierIntegrationTests : IntegrationTestBase
{
    public EnableCarrierIntegrationTests(TestWebApplicationFactory factory) : base(factory) { }

    [Fact]
    public async Task EnableCarrier_WithExistingCarrier_ShouldReturn200()
    {
        var carrier = await AddCarrierAsync("Carrier To Enable", isEnabled: false);

        var response = await Client.PatchAsJsonAsync($"/api/v1/carriers/{carrier.Id}/enable", new { id = carrier.Id });

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var root = await ReadJsonAsync(response);
        GetBoolean(root, "isEnabled").ShouldBeTrue();
    }
}
