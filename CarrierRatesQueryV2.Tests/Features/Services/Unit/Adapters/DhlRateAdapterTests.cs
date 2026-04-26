using CarrierRatesQueryV2.Core.Contracts.Rates;
using CarrierRatesQueryV2.Core.Rates.Adapters;
using CarrierRatesQueryV2.Core.Rates.Clients;
using Xunit;

namespace CarrierRatesQueryV2.Tests.Features.Services.Unit.Adapters;

public class DhlRateAdapterTests
{
    private readonly DhlRateAdapter _adapter = new();

    [Fact]
    public void Adapt_ValidResponse_ReturnsNormalizedQuote()
    {
        var response = new MockDhlRateResponse(
            Carrier: "DHL",
            Options: new List<MockDhlServiceOption>
            {
                new("DHL Express", DateTime.UtcNow.AddDays(2), 18.50m, "USD"),
                new("DHL Economy", DateTime.UtcNow.AddDays(5), 8.99m, "USD")
            });

        var result = _adapter.Adapt(response);

        Assert.NotNull(result);
        Assert.Equal("DHL", result.Carrier);
        Assert.Equal(2, result.RateOptions.Count);
    }

    [Fact]
    public void Adapt_EmptyOptions_ReturnsQuoteWithEmptyOptions()
    {
        var response = new MockDhlRateResponse(
            Carrier: "DHL",
            Options: new List<MockDhlServiceOption>());

        var result = _adapter.Adapt(response);

        Assert.NotNull(result);
        Assert.Equal("DHL", result.Carrier);
        Assert.Empty(result.RateOptions);
    }

    [Fact]
    public void Adapt_SingleOption_ReturnsQuote()
    {
        var response = new MockDhlRateResponse(
            Carrier: "DHL",
            Options: new List<MockDhlServiceOption>
            {
                new("DHL Express Worldwide", DateTime.UtcNow.AddDays(1), 35.00m, "USD")
            });

        var result = _adapter.Adapt(response);

        Assert.Single(result.RateOptions);
        Assert.Equal("DHL Express Worldwide", result.RateOptions[0].ServiceName);
        Assert.Equal(35.00m, result.RateOptions[0].Price.Amount);
    }
}