using CarrierRatesQueryV2.Core;
using CarrierRatesQueryV2.Core.Contracts.Rates;
using CarrierRatesQueryV2.Core.Interfaces.Rates;
using CarrierRatesQueryV2.Core.Interfaces.Rates.Clients;
using CarrierRatesQueryV2.Core.Rates;
using CarrierRatesQueryV2.Core.Rates.Clients;
using CarrierRatesQueryV2.Core.Rates.Strategies;
using NSubstitute;
using Shouldly;
using Xunit;

namespace CarrierRatesQueryV2.Tests.Features.Services.Unit.Strategies;

public class DhlRateStrategyTests
{
    private static CarrierContext CreateCarrierContext(string endpoint = "http://localhost:5135/api/dhl/rates")
    {
        return new CarrierContext(
            Id: Guid.NewGuid(),
            Name: "DHL",
            Slug: "dhl",
            UpdatedAtUtc: DateTime.UtcNow,
            Endpoints: [new CarrierEndpointConfig("Rates", endpoint)]);
    }

    private static RateQuery CreateRateQuery()
    {
        return new RateQuery(
            Origin: new Location("12345", "US"),
            Destination: new Location("67890", "US"),
            Package: new Package(5m, new PackageDimensions(10m, 5m, 5m)));
    }

    [Fact]
    public async Task TryGetRatesAsync_CacheHit_ReturnsCachedQuote()
    {
        var mockCache = Substitute.For<IRateCache>();
        var mockClient = Substitute.For<IMockDhlRatesClient>();
        var mockAdapter = Substitute.For<ICarrierRateAdapter<MockDhlRateResponse>>();

        var carrier = CreateCarrierContext();
        var query = CreateRateQuery();
        var cachedQuote = new ShippingRateQuote("DHL", []);

        mockCache.GetAsync(carrier, query, CancellationToken.None).Returns(cachedQuote);

        var strategy = new DhlRateStrategy(mockClient, mockAdapter, mockCache);

        var result = await strategy.TryGetRatesAsync(carrier, query, CancellationToken.None);

        result.ShouldBe(cachedQuote);
        await mockClient.Received(0).GetRatesAsync(Arg.Any<string>(), Arg.Any<MockDhlRateRequest>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task TryGetRatesAsync_CacheMiss_CallsApiAndCaches()
    {
        var mockCache = Substitute.For<IRateCache>();
        var mockClient = Substitute.For<IMockDhlRatesClient>();
        var mockAdapter = Substitute.For<ICarrierRateAdapter<MockDhlRateResponse>>();

        var carrier = CreateCarrierContext();
        var query = CreateRateQuery();
        var apiResponse = new MockDhlRateResponse("DHL", [new MockDhlServiceOption("Express", DateTime.UtcNow.AddDays(2), 18.50m, "USD")]);
        var adaptedQuote = new ShippingRateQuote("DHL", [new RateOption("Express", DateTime.UtcNow.AddDays(2), new Money(18.50m, "USD"))]);

        mockCache.GetAsync(carrier, query, CancellationToken.None).Returns((ShippingRateQuote?)null);
        mockClient.GetRatesAsync(Arg.Any<string>(), Arg.Any<MockDhlRateRequest>(), Arg.Any<CancellationToken>()).Returns(apiResponse);
        mockAdapter.Adapt(apiResponse).Returns(adaptedQuote);

        var strategy = new DhlRateStrategy(mockClient, mockAdapter, mockCache);

        var result = await strategy.TryGetRatesAsync(carrier, query, CancellationToken.None);

        result.ShouldNotBeNull();
        result.Carrier.ShouldBe("DHL");
        await mockClient.Received(1).GetRatesAsync(Arg.Any<string>(), Arg.Any<MockDhlRateRequest>(), Arg.Any<CancellationToken>());
        await mockCache.Received(1).SetAsync(carrier, query, result, CancellationToken.None);
    }

    [Fact]
    public async Task TryGetRatesAsync_NoEndpoint_ReturnsNull()
    {
        var mockCache = Substitute.For<IRateCache>();
        var mockClient = Substitute.For<IMockDhlRatesClient>();
        var mockAdapter = Substitute.For<ICarrierRateAdapter<MockDhlRateResponse>>();

        var carrier = new CarrierContext(
            Id: Guid.NewGuid(),
            Name: "DHL",
            Slug: "dhl",
            UpdatedAtUtc: DateTime.UtcNow,
            Endpoints: []);
        var query = CreateRateQuery();

        var strategy = new DhlRateStrategy(mockClient, mockAdapter, mockCache);

        var result = await strategy.TryGetRatesAsync(carrier, query, CancellationToken.None);

        result.ShouldBeNull();
        await mockClient.Received(0).GetRatesAsync(Arg.Any<string>(), Arg.Any<MockDhlRateRequest>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task TryGetRatesAsync_NoRatesEndpoint_ReturnsNull()
    {
        var mockCache = Substitute.For<IRateCache>();
        var mockClient = Substitute.For<IMockDhlRatesClient>();
        var mockAdapter = Substitute.For<ICarrierRateAdapter<MockDhlRateResponse>>();

        var carrier = new CarrierContext(
            Id: Guid.NewGuid(),
            Name: "DHL",
            Slug: "dhl",
            UpdatedAtUtc: DateTime.UtcNow,
            Endpoints: [new CarrierEndpointConfig("Track", "http://localhost:5135/api/dhl/track")]);
        var query = CreateRateQuery();

        var strategy = new DhlRateStrategy(mockClient, mockAdapter, mockCache);

        var result = await strategy.TryGetRatesAsync(carrier, query, CancellationToken.None);

        result.ShouldBeNull();
    }
}