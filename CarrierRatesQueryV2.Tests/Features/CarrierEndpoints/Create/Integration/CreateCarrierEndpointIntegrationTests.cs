using CarrierRatesQueryV2.Tests.Infrastructure;
using Shouldly;
using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace CarrierRatesQueryV2.Tests.Features.CarrierEndpoints.Create.Integration;

[Collection("IntegrationTests")]
public class CreateCarrierEndpointIntegrationTests : IntegrationTestBase
{
    public CreateCarrierEndpointIntegrationTests(TestWebApplicationFactory factory) : base(factory) { }

    [Fact]
    public async Task CreateCarrierEndpoint_WithValidRequest_ShouldReturn201()
    {
        var carrier = await AddCarrierAsync("Carrier Endpoint Create");

        var response = await Client.PostAsJsonAsync($"/api/v1/carriers/{carrier.Id}/endpoints", new
        {
            carrierId = carrier.Id,
            operation = "Rates",
            endpoint = "https://example.test/rates"
        });

        await ShouldHaveStatusAsync(response, HttpStatusCode.Created);
        var root = await ReadJsonAsync(response);
        GetGuid(root, "carrierId").ShouldBe(carrier.Id);
    }
}
