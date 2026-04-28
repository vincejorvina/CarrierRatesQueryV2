using CarrierRatesQueryV2.Tests.Infrastructure;
using Shouldly;
using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace CarrierRatesQueryV2.Tests.Features.Rates.QueryByCarrierSlug.Integration;

[Collection("IntegrationTests")]
public class QueryByCarrierSlugIntegrationTests : IntegrationTestBase
{
    public QueryByCarrierSlugIntegrationTests(TestWebApplicationFactory factory) : base(factory) { }

    [Fact]
    public async Task QueryRatesByCarrierSlug_WithValidRequest_ShouldReturn200()
    {
        var response = await Client.PostAsJsonAsync("/api/v1/rates/carrier/slug/fedex", CreateRateRequest(new { carrierSlug = "fedex" }));

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var root = await ReadJsonAsync(response);
        GetString(root, "carrier").ShouldBe("FedEx");
    }
}
