using CarrierRatesQueryV2.Core.Contracts.Rates;
using CarrierRatesQueryV2.Core.Rates.Adapters;
using CarrierRatesQueryV2.Core.Rates.Clients;
using Xunit;

namespace CarrierRatesQueryV2.Tests.Features.Services.Unit.Adapters;

public class UpsRateAdapterTests
{
    private readonly UpsRateAdapter _adapter = new();

    [Fact]
    public void Adapt_ValidResponse_ReturnsNormalizedQuote()
    {
        var response = new MockUpsRateResponse(
            Carrier: "UPS",
            Services: new List<MockUpsServiceOption>
            {
                new("UPS Ground", DateTime.UtcNow.AddDays(4), 10.50m, "USD"),
                new("UPS 2nd Day Air", DateTime.UtcNow.AddDays(2), 22.75m, "USD")
            });

        var result = _adapter.Adapt(response);

        Assert.NotNull(result);
        Assert.Equal("UPS", result.Carrier);
        Assert.Equal(2, result.RateOptions.Count);
    }

    [Fact]
    public void Adapt_EmptyServices_ReturnsQuoteWithEmptyOptions()
    {
        var response = new MockUpsRateResponse(
            Carrier: "UPS",
            Services: new List<MockUpsServiceOption>());

        var result = _adapter.Adapt(response);

        Assert.NotNull(result);
        Assert.Equal("UPS", result.Carrier);
        Assert.Empty(result.RateOptions);
    }

    [Fact]
    public void Adapt_SingleService_ReturnsQuote()
    {
        var response = new MockUpsRateResponse(
            Carrier: "UPS",
            Services: new List<MockUpsServiceOption>
            {
                new("UPS Next Day Air", DateTime.UtcNow.AddDays(1), 55.00m, "USD")
            });

        var result = _adapter.Adapt(response);

        Assert.Single(result.RateOptions);
        Assert.Equal("UPS Next Day Air", result.RateOptions[0].ServiceName);
        Assert.Equal(55.00m, result.RateOptions[0].Price.Amount);
    }
}