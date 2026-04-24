using CarrierRatesQueryV2.Api.Features.Carriers.GetAll;
using CarrierRatesQueryV2.Tests.Infrastructure;
using Shouldly;

namespace CarrierRatesQueryV2.Tests.Features.Carriers;

public class GetAllCarriersHandlerTests : TestBase<Endpoint>
{
    [Fact]
    public async Task Handle_ShouldReturnEmptyList_WhenNoCarriers()
    {
        // Act
        await Endpoint.HandleAsync(CancellationToken.None);

        // Assert
        var response = Endpoint.Response;
        response.ShouldBeEmpty();
    }
}