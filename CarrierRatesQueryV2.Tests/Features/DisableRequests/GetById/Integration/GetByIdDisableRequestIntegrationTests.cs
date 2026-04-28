using CarrierRatesQueryV2.Tests.Infrastructure;
using Shouldly;
using System.Net;
using Xunit;

namespace CarrierRatesQueryV2.Tests.Features.DisableRequests.GetById.Integration;

[Collection("IntegrationTests")]
public class GetByIdDisableRequestIntegrationTests : IntegrationTestBase
{
    public GetByIdDisableRequestIntegrationTests(TestWebApplicationFactory factory) : base(factory) { }

    [Fact]
    public async Task GetDisableRequestById_WithSeededRequest_ShouldReturn200()
    {
        var disableRequest = await GetPendingDisableRequestAsync();

        var response = await Client.GetAsync($"/api/v1/disable-requests/{disableRequest.Id}");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var root = await ReadJsonAsync(response);
        GetGuid(root, "id").ShouldBe(disableRequest.Id);
    }

    [Fact]
    public async Task GetDisableRequestById_WithNonExistentId_ShouldReturn404()
    {
        var response = await Client.GetAsync($"/api/v1/disable-requests/{Guid.NewGuid()}");

        await ShouldHaveStatusAsync(response, HttpStatusCode.NotFound);
    }
}
