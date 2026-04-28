using CarrierRatesQueryV2.Tests.Infrastructure;
using Shouldly;
using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace CarrierRatesQueryV2.Tests.Features.Carriers.Create.Integration;

[Collection("IntegrationTests")]
public class CreateCarrierIntegrationTests : IntegrationTestBase
{
    public CreateCarrierIntegrationTests(TestWebApplicationFactory factory) : base(factory) { }

    [Fact]
    public async Task CreateCarrier_WithValidRequest_ShouldReturn201()
    {
        var response = await Client.PostAsJsonAsync("/api/v1/carriers", new { name = "Integration Carrier", isEnabled = true });

        await ShouldHaveStatusAsync(response, HttpStatusCode.Created);
        var root = await ReadJsonAsync(response);
        GetString(root, "name").ShouldBe("Integration Carrier");
    }
}
