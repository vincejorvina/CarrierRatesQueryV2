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

public class FedExRateStrategyTests
{
    private static CarrierContext CreateCarrierContext(string endpoint = "http://localhost:5133/api/fedex/rates")
    {
        return new CarrierContext(
            Id: Guid.NewGuid(),
            Name: "FedEx",
            Slug: "fedex",
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
        var mockClient = Substitute.For<IMockFedExRatesClient>();
        var mockAdapter = Substitute.For<ICarrierRateAdapter<MockFedExRateResponse>>();

        var carrier = CreateCarrierContext();
        var query = CreateRateQuery();
        var cachedQuote = new ShippingRateQuote("FedEx", []);

        mockCache.GetAsync(carrier, query, CancellationToken.None).Returns(cachedQuote);

        var strategy = new FedExRateStrategy(mockClient, mockAdapter, mockCache);

        var result = await strategy.TryGetRatesAsync(carrier, query, CancellationToken.None);

        result.ShouldBe(cachedQuote);
        await mockClient.Received(0).GetRatesAsync(Arg.Any<string>(), Arg.Any<MockFedExRateRequest>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task TryGetRatesAsync_CacheMiss_CallsApiAndCaches()
    {
        var mockCache = Substitute.For<IRateCache>();
        var mockClient = Substitute.For<IMockFedExRatesClient>();
        var mockAdapter = Substitute.For<ICarrierRateAdapter<MockFedExRateResponse>>();

        var carrier = CreateCarrierContext();
        var query = CreateRateQuery();
        var apiResponse = new MockFedExRateResponse("FedEx", [new MockFedExServiceOption("Ground", "2026-04-30", 12.99m)]);
        var adaptedQuote = new ShippingRateQuote("FedEx", [new RateOption("Ground", DateTime.UtcNow.AddDays(3), new Money(12.99m, "USD"))]);

        mockCache.GetAsync(carrier, query, CancellationToken.None).Returns((ShippingRateQuote?)null);
        mockClient.GetRatesAsync(Arg.Any<string>(), Arg.Any<MockFedExRateRequest>(), Arg.Any<CancellationToken>()).Returns(apiResponse);
        mockAdapter.Adapt(apiResponse).Returns(adaptedQuote);

        var strategy = new FedExRateStrategy(mockClient, mockAdapter, mockCache);

        var result = await strategy.TryGetRatesAsync(carrier, query, CancellationToken.None);

        result.ShouldNotBeNull();
        result.Carrier.ShouldBe("FedEx");
        await mockClient.Received(1).GetRatesAsync(Arg.Any<string>(), Arg.Any<MockFedExRateRequest>(), Arg.Any<CancellationToken>());
        await mockCache.Received(1).SetAsync(carrier, query, result, CancellationToken.None);
    }

    [Fact]
    public async Task TryGetRatesAsync_NoEndpoint_ReturnsNull()
    {
        var mockCache = Substitute.For<IRateCache>();
        var mockClient = Substitute.For<IMockFedExRatesClient>();
        var mockAdapter = Substitute.For<ICarrierRateAdapter<MockFedExRateResponse>>();

        var carrier = new CarrierContext(
            Id: Guid.NewGuid(),
            Name: "FedEx",
            Slug: "fedex",
            UpdatedAtUtc: DateTime.UtcNow,
            Endpoints: []);
        var query = CreateRateQuery();

        var strategy = new FedExRateStrategy(mockClient, mockAdapter, mockCache);

        var result = await strategy.TryGetRatesAsync(carrier, query, CancellationToken.None);

        result.ShouldBeNull();
        await mockClient.Received(0).GetRatesAsync(Arg.Any<string>(), Arg.Any<MockFedExRateRequest>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task TryGetRatesAsync_NoRatesEndpoint_ReturnsNull()
    {
        var mockCache = Substitute.For<IRateCache>();
        var mockClient = Substitute.For<IMockFedExRatesClient>();
        var mockAdapter = Substitute.For<ICarrierRateAdapter<MockFedExRateResponse>>();

        var carrier = new CarrierContext(
            Id: Guid.NewGuid(),
            Name: "FedEx",
            Slug: "fedex",
            UpdatedAtUtc: DateTime.UtcNow,
            Endpoints: [new CarrierEndpointConfig("Other", "http://localhost:5133/api/fedex/other")]);
        var query = CreateRateQuery();

        var strategy = new FedExRateStrategy(mockClient, mockAdapter, mockCache);

        var result = await strategy.TryGetRatesAsync(carrier, query, CancellationToken.None);

        result.ShouldBeNull();
    }
}