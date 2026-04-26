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

public class UpsRateStrategyTests
{
    private static CarrierContext CreateCarrierContext(string endpoint = "http://localhost:5134/api/ups/rates")
    {
        return new CarrierContext(
            Id: Guid.NewGuid(),
            Name: "UPS",
            Slug: "ups",
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
        var mockClient = Substitute.For<IMockUpsRatesClient>();
        var mockAdapter = Substitute.For<ICarrierRateAdapter<MockUpsRateResponse>>();

        var carrier = CreateCarrierContext();
        var query = CreateRateQuery();
        var cachedQuote = new ShippingRateQuote("UPS", []);

        mockCache.GetAsync(carrier, query, CancellationToken.None).Returns(cachedQuote);

        var strategy = new UpsRateStrategy(mockClient, mockAdapter, mockCache);

        var result = await strategy.TryGetRatesAsync(carrier, query, CancellationToken.None);

        result.ShouldBe(cachedQuote);
        await mockClient.Received(0).GetRatesAsync(Arg.Any<string>(), Arg.Any<MockUpsRateRequest>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task TryGetRatesAsync_CacheMiss_CallsApiAndCaches()
    {
        var mockCache = Substitute.For<IRateCache>();
        var mockClient = Substitute.For<IMockUpsRatesClient>();
        var mockAdapter = Substitute.For<ICarrierRateAdapter<MockUpsRateResponse>>();

        var carrier = CreateCarrierContext();
        var query = CreateRateQuery();
        var apiResponse = new MockUpsRateResponse("UPS", [new MockUpsServiceOption("Ground", DateTime.UtcNow.AddDays(4), 10.50m, "USD")]);
        var adaptedQuote = new ShippingRateQuote("UPS", [new RateOption("Ground", DateTime.UtcNow.AddDays(4), new Money(10.50m, "USD"))]);

        mockCache.GetAsync(carrier, query, CancellationToken.None).Returns((ShippingRateQuote?)null);
        mockClient.GetRatesAsync(Arg.Any<string>(), Arg.Any<MockUpsRateRequest>(), Arg.Any<CancellationToken>()).Returns(apiResponse);
        mockAdapter.Adapt(apiResponse).Returns(adaptedQuote);

        var strategy = new UpsRateStrategy(mockClient, mockAdapter, mockCache);

        var result = await strategy.TryGetRatesAsync(carrier, query, CancellationToken.None);

        result.ShouldNotBeNull();
        result.Carrier.ShouldBe("UPS");
        await mockClient.Received(1).GetRatesAsync(Arg.Any<string>(), Arg.Any<MockUpsRateRequest>(), Arg.Any<CancellationToken>());
        await mockCache.Received(1).SetAsync(carrier, query, result, CancellationToken.None);
    }

    [Fact]
    public async Task TryGetRatesAsync_NoEndpoint_ReturnsNull()
    {
        var mockCache = Substitute.For<IRateCache>();
        var mockClient = Substitute.For<IMockUpsRatesClient>();
        var mockAdapter = Substitute.For<ICarrierRateAdapter<MockUpsRateResponse>>();

        var carrier = new CarrierContext(
            Id: Guid.NewGuid(),
            Name: "UPS",
            Slug: "ups",
            UpdatedAtUtc: DateTime.UtcNow,
            Endpoints: []);
        var query = CreateRateQuery();

        var strategy = new UpsRateStrategy(mockClient, mockAdapter, mockCache);

        var result = await strategy.TryGetRatesAsync(carrier, query, CancellationToken.None);

        result.ShouldBeNull();
        await mockClient.Received(0).GetRatesAsync(Arg.Any<string>(), Arg.Any<MockUpsRateRequest>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task TryGetRatesAsync_ApiThrows_PropagatesException()
    {
        var mockCache = Substitute.For<IRateCache>();
        var mockClient = Substitute.For<IMockUpsRatesClient>();
        var mockAdapter = Substitute.For<ICarrierRateAdapter<MockUpsRateResponse>>();

        var carrier = CreateCarrierContext();
        var query = CreateRateQuery();

        mockCache.GetAsync(carrier, query, CancellationToken.None).Returns((ShippingRateQuote?)null);
        mockClient.GetRatesAsync(Arg.Any<string>(), Arg.Any<MockUpsRateRequest>(), Arg.Any<CancellationToken>())
            .Returns(async _ => throw new HttpRequestException("Network error"));

        var strategy = new UpsRateStrategy(mockClient, mockAdapter, mockCache);

        await Assert.ThrowsAsync<HttpRequestException>(() =>
            strategy.TryGetRatesAsync(carrier, query, CancellationToken.None));
    }
}