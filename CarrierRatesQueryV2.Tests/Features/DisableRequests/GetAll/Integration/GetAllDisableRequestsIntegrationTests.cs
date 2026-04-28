using CarrierRatesQueryV2.Tests.Infrastructure;
using Shouldly;
using System.Net;
using System.Text.Json;
using Xunit;

namespace CarrierRatesQueryV2.Tests.Features.DisableRequests.GetAll.Integration;

[Collection("IntegrationTests")]
public class GetAllDisableRequestsIntegrationTests : IntegrationTestBase
{
    public GetAllDisableRequestsIntegrationTests(TestWebApplicationFactory factory) : base(factory) { }

    [Fact]
    public async Task GetAllDisableRequests_WithRoleHeader_ShouldReturn200()
    {
        var request = CreateRoleRequest(HttpMethod.Get, "/api/v1/disable-requests", "User");

        var response = await Client.SendAsync(request);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var root = await ReadJsonAsync(response);
        root.ValueKind.ShouldBe(JsonValueKind.Array);
    }
}
