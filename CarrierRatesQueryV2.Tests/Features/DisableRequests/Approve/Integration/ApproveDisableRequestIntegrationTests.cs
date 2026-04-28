using CarrierRatesQueryV2.Tests.Infrastructure;
using Shouldly;
using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace CarrierRatesQueryV2.Tests.Features.DisableRequests.Approve.Integration;

[Collection("IntegrationTests")]
public class ApproveDisableRequestIntegrationTests : IntegrationTestBase
{
    public ApproveDisableRequestIntegrationTests(TestWebApplicationFactory factory) : base(factory) { }

    [Fact]
    public async Task ApproveDisableRequest_WithAdminRequest_ShouldReturn200()
    {
        var carrier = await AddCarrierAsync("Carrier Disable Request Approve");
        var disableRequest = await AddDisableRequestAsync(carrier.Id);
        var request = CreateRoleRequest(HttpMethod.Patch, $"/api/v1/disable-requests/{disableRequest.Id}/approve", "Admin", "integration-admin");
        request.Content = JsonContent.Create(new { disableRequestId = disableRequest.Id });

        var response = await Client.SendAsync(request);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var root = await ReadJsonAsync(response);
        GetString(root, "status").ShouldBe("Approved");
    }
}
