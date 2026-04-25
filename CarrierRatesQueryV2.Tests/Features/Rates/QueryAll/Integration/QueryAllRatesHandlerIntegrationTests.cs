using CarrierRatesQueryV2.Api.Features.Rates.QueryAll;
using CarrierRatesQueryV2.Tests.Infrastructure;
using Shouldly;
using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace CarrierRatesQueryV2.Tests.Features.Rates.QueryAll.Integration;

[Collection("IntegrationTests")]
public class QueryAllRatesHandlerIntegrationTests : IntegrationTestBase
{
    public QueryAllRatesHandlerIntegrationTests(TestWebApplicationFactory factory) : base(factory) { }

    [Fact]
    public async Task GetAllRates_WithValidRequest_ShouldReturn200()
    {
        var request = new Request(
            new LocationRequest("12345", "US"),
            new LocationRequest("67890", "US"),
            new PackageRequest(5, new PackageDimensionsRequest(10, 5, 5))
        );

        var response = await Client.PostAsJsonAsync("/api/v1/rates", request);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<List<RateQuoteResponse>>();
        result.ShouldNotBeNull();
    }

    [Fact]
    public async Task GetAllRates_AfterReset_ShouldStillReturn200()
    {
        var request = new Request(
            new LocationRequest("12345", "US"),
            new LocationRequest("67890", "US"),
            new PackageRequest(5, new PackageDimensionsRequest(10, 5, 5))
        );

        var response = await Client.PostAsJsonAsync("/api/v1/rates", request);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }
}