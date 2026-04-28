using CarrierRatesQueryV2.Tests.Infrastructure;
using Shouldly;
using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace CarrierRatesQueryV2.Tests.Features.Carriers.Disable.Integration;

[Collection("IntegrationTests")]
public class DisableCarrierIntegrationTests : IntegrationTestBase
{
    public DisableCarrierIntegrationTests(TestWebApplicationFactory factory) : base(factory) { }

    [Fact]
    public async Task DisableCarrier_WithAdminRequest_ShouldReturn200()
    {
        var carrier = await AddCarrierAsync("Carrier To Disable");
        var request = CreateRoleRequest(HttpMethod.Patch, $"/api/v1/carriers/{carrier.Id}/disable", "Admin", "integration-admin");
        request.Content = JsonContent.Create(new { id = carrier.Id, reason = "Integration maintenance" });

        var response = await Client.SendAsync(request);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var root = await ReadJsonAsync(response);
        GetBoolean(root, "isEnabled").ShouldBeFalse();
    }
}
