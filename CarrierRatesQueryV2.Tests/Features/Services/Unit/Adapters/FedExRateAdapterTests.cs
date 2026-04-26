using CarrierRatesQueryV2.Core.Contracts.Rates;
using CarrierRatesQueryV2.Core.Rates.Adapters;
using CarrierRatesQueryV2.Core.Rates.Clients;
using Xunit;

namespace CarrierRatesQueryV2.Tests.Features.Services.Unit.Adapters;

public class FedExRateAdapterTests
{
    private readonly FedExRateAdapter _adapter = new();

    [Fact]
    public void Adapt_ValidResponse_ReturnsNormalizedQuote()
    {
        var response = new MockFedExRateResponse(
            Carrier: "FedEx",
            ServiceOptions: new List<MockFedExServiceOption>
            {
                new("FedEx Ground", "2026-04-30", 12.99m),
                new("FedEx Express", "2026-04-28", 25.50m)
            });

        var result = _adapter.Adapt(response);

        Assert.NotNull(result);
        Assert.Equal("FedEx", result.Carrier);
        Assert.Equal(2, result.RateOptions.Count);
    }

    [Fact]
    public void Adapt_EmptyServiceOptions_ReturnsQuoteWithEmptyOptions()
    {
        var response = new MockFedExRateResponse(
            Carrier: "FedEx",
            ServiceOptions: new List<MockFedExServiceOption>());

        var result = _adapter.Adapt(response);

        Assert.NotNull(result);
        Assert.Equal("FedEx", result.Carrier);
        Assert.Empty(result.RateOptions);
    }

    [Fact]
    public void Adapt_SingleServiceOption_ReturnsQuote()
    {
        var response = new MockFedExRateResponse(
            Carrier: "FedEx",
            ServiceOptions: new List<MockFedExServiceOption>
            {
                new("FedEx Priority Overnight", "2026-04-27", 45.00m)
            });

        var result = _adapter.Adapt(response);

        Assert.Single(result.RateOptions);
        Assert.Equal("FedEx Priority Overnight", result.RateOptions[0].ServiceName);
        Assert.Equal(45.00m, result.RateOptions[0].Price.Amount);
    }
}