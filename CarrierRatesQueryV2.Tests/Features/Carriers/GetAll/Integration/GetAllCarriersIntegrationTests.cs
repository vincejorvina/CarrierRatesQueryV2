using CarrierRatesQueryV2.Tests.Infrastructure;
using Shouldly;
using System.Net;
using Xunit;

namespace CarrierRatesQueryV2.Tests.Features.Carriers.GetAll.Integration;

[Collection("IntegrationTests")]
public class GetAllCarriersIntegrationTests : IntegrationTestBase
{
    public GetAllCarriersIntegrationTests(TestWebApplicationFactory factory) : base(factory) { }

    [Fact]
    public async Task GetAllCarriers_WithSeededData_ShouldReturn200()
    {
        var response = await Client.GetAsync("/api/v1/carriers");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var root = await ReadJsonAsync(response);
        root.GetArrayLength().ShouldBeGreaterThanOrEqualTo(3);
    }
}
