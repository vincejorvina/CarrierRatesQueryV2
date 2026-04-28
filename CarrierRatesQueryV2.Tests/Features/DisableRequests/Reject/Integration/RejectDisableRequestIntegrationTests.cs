using CarrierRatesQueryV2.Tests.Infrastructure;
using Shouldly;
using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace CarrierRatesQueryV2.Tests.Features.DisableRequests.Reject.Integration;

[Collection("IntegrationTests")]
public class RejectDisableRequestIntegrationTests : IntegrationTestBase
{
    public RejectDisableRequestIntegrationTests(TestWebApplicationFactory factory) : base(factory) { }

    [Fact]
    public async Task RejectDisableRequest_WithAdminRequest_ShouldReturn200()
    {
        var disableRequest = await GetPendingDisableRequestAsync();
        var request = CreateRoleRequest(HttpMethod.Patch, $"/api/v1/disable-requests/{disableRequest.Id}/reject", "Admin", "integration-admin");
        request.Content = JsonContent.Create(new { disableRequestId = disableRequest.Id });

        var response = await Client.SendAsync(request);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var root = await ReadJsonAsync(response);
        GetString(root, "status").ShouldBe("Rejected");
    }
}
