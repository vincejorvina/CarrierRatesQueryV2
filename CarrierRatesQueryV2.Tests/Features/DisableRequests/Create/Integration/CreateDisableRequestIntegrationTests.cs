using CarrierRatesQueryV2.Tests.Infrastructure;
using Shouldly;
using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace CarrierRatesQueryV2.Tests.Features.DisableRequests.Create.Integration;

[Collection("IntegrationTests")]
public class CreateDisableRequestIntegrationTests : IntegrationTestBase
{
    public CreateDisableRequestIntegrationTests(TestWebApplicationFactory factory) : base(factory) { }

    [Fact]
    public async Task CreateDisableRequest_WithValidUserRequest_ShouldReturn201()
    {
        var carrier = await AddCarrierAsync("Carrier Disable Request Create");
        var request = CreateRoleRequest(HttpMethod.Post, $"/api/v1/carriers/{carrier.Id}/disable-requests", "User", "integration-user");
        request.Content = JsonContent.Create(new { carrierId = carrier.Id, reason = "Temporary maintenance" });

        var response = await Client.SendAsync(request);

        await ShouldHaveStatusAsync(response, HttpStatusCode.Created);
        var root = await ReadJsonAsync(response);
        GetString(root, "status").ShouldBe("Pending");
    }
}
