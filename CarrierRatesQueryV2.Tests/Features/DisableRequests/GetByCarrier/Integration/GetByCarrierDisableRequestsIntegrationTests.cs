using CarrierRatesQueryV2.Tests.Infrastructure;
using Shouldly;
using System.Net;
using Xunit;

namespace CarrierRatesQueryV2.Tests.Features.DisableRequests.GetByCarrier.Integration;

[Collection("IntegrationTests")]
public class GetByCarrierDisableRequestsIntegrationTests : IntegrationTestBase
{
    public GetByCarrierDisableRequestsIntegrationTests(TestWebApplicationFactory factory) : base(factory) { }

    [Fact]
    public async Task GetDisableRequestsByCarrier_WithRoleHeader_ShouldReturn200()
    {
        var disableRequest = await GetPendingDisableRequestAsync();
        var request = CreateRoleRequest(HttpMethod.Get, $"/api/v1/carriers/{disableRequest.CarrierId}/disable-requests", "User");

        var response = await Client.SendAsync(request);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var root = await ReadJsonAsync(response);
        root.GetArrayLength().ShouldBeGreaterThanOrEqualTo(1);
    }
}
