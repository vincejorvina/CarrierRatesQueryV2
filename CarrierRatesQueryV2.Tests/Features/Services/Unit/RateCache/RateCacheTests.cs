using CarrierRatesQueryV2.Core;
using CarrierRatesQueryV2.Core.Contracts.Rates;
using CarrierRatesQueryV2.Core.Interfaces.Rates;
using CarrierRatesQueryV2.Core.Rates;
using CarrierRatesQueryV2.Tests.Infrastructure;
using Microsoft.Extensions.Caching.Memory;
using Xunit;

namespace CarrierRatesQueryV2.Tests.Features.Services.Unit.RateCache;

public class RateCacheTests
{
    private static CarrierContext CreateCarrierContext(string slug = "fedex")
    {
        return new CarrierContext(
            Id: Guid.NewGuid(),
            Name: slug == "fedex" ? "FedEx" : slug.ToUpper(),
            Slug: slug,
            UpdatedAtUtc: DateTime.UtcNow,
            Endpoints: [new CarrierEndpointConfig("Rates", $"http://localhost:5133/api/{slug}/rates")]);
    }

    private static RateQuery CreateRateQuery(decimal weight = 5m)
    {
        return new RateQuery(
            Origin: new Location("12345", "US"),
            Destination: new Location("67890", "US"),
            Package: new Package(weight, new PackageDimensions(10m, 5m, 5m)));
    }

    [Fact]
    public async Task GetAsync_FirstRequest_ReturnsNull()
    {
        var cache = new MemoryCache(new MemoryCacheOptions());
        var sut = new MemoryRateCache(cache);

        var carrier = CreateCarrierContext();
        var query = CreateRateQuery();

        var result = await sut.GetAsync(carrier, query, CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task SetAsync_ThenGet_ReturnsQuote()
    {
        var cache = new MemoryCache(new MemoryCacheOptions());
        var sut = new MemoryRateCache(cache);

        var carrier = CreateCarrierContext();
        var query = CreateRateQuery();
        var quote = new ShippingRateQuote("FedEx", [new RateOption("Ground", DateTime.UtcNow.AddDays(3), new Money(12.99m, "USD"))]);

        await sut.SetAsync(carrier, query, quote, CancellationToken.None);
        var result = await sut.GetAsync(carrier, query, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal("FedEx", result.Carrier);
    }

    [Fact]
    public async Task SetAsync_DifferentQuery_ReturnsNull()
    {
        var cache = new MemoryCache(new MemoryCacheOptions());
        var sut = new MemoryRateCache(cache);

        var carrier = CreateCarrierContext();
        var query1 = CreateRateQuery(5m);
        var query2 = CreateRateQuery(10m);
        var quote = new ShippingRateQuote("FedEx", [new RateOption("Ground", DateTime.UtcNow.AddDays(3), new Money(12.99m, "USD"))]);

        await sut.SetAsync(carrier, query1, quote, CancellationToken.None);
        var result = await sut.GetAsync(carrier, query2, CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task SetAsync_DifferentCarrier_ReturnsNull()
    {
        var cache = new MemoryCache(new MemoryCacheOptions());
        var sut = new MemoryRateCache(cache);

        var fedex = CreateCarrierContext("fedex");
        var ups = CreateCarrierContext("ups");
        var query = CreateRateQuery();
        var quote = new ShippingRateQuote("FedEx", [new RateOption("Ground", DateTime.UtcNow.AddDays(3), new Money(12.99m, "USD"))]);

        await sut.SetAsync(fedex, query, quote, CancellationToken.None);
        var result = await sut.GetAsync(ups, query, CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task SetAsync_MultipleCarriersAndQueries_ReturnsCorrectCache()
    {
        var cache = new MemoryCache(new MemoryCacheOptions());
        var sut = new MemoryRateCache(cache);

        var fedex = CreateCarrierContext("fedex");
        var ups = CreateCarrierContext("ups");
        var query1 = CreateRateQuery(5m);
        var query2 = CreateRateQuery(10m);

        var fedexQuote = new ShippingRateQuote("FedEx", [new RateOption("Ground", DateTime.UtcNow.AddDays(3), new Money(12.99m, "USD"))]);
        var upsQuote = new ShippingRateQuote("UPS", [new RateOption("Ground", DateTime.UtcNow.AddDays(4), new Money(10.99m, "USD"))]);

        await sut.SetAsync(fedex, query1, fedexQuote, CancellationToken.None);
        await sut.SetAsync(ups, query1, upsQuote, CancellationToken.None);
        await sut.SetAsync(fedex, query2, fedexQuote, CancellationToken.None);

        var fedexQuery1Result = await sut.GetAsync(fedex, query1, CancellationToken.None);
        var upsQuery1Result = await sut.GetAsync(ups, query1, CancellationToken.None);
        var fedexQuery2Result = await sut.GetAsync(fedex, query2, CancellationToken.None);

        Assert.NotNull(fedexQuery1Result);
        Assert.Equal("FedEx", fedexQuery1Result.Carrier);
        Assert.NotNull(upsQuery1Result);
        Assert.Equal("UPS", upsQuery1Result.Carrier);
        Assert.NotNull(fedexQuery2Result);
        Assert.Equal("FedEx", fedexQuery2Result.Carrier);
    }

    [Fact]
    public async Task SetAsync_CarrierWithNullUpdatedAtUtc_CacheStillWorks()
    {
        var cache = new MemoryCache(new MemoryCacheOptions());
        var sut = new MemoryRateCache(cache);

        var carrier = new CarrierContext(
            Id: Guid.NewGuid(),
            Name: "Test",
            Slug: "test",
            UpdatedAtUtc: null,
            Endpoints: [new CarrierEndpointConfig("Rates", "http://localhost:5000/rates")]);
        var query = CreateRateQuery();
        var quote = new ShippingRateQuote("Test", [new RateOption("Standard", DateTime.UtcNow.AddDays(3), new Money(5.00m, "USD"))]);

        await sut.SetAsync(carrier, query, quote, CancellationToken.None);
        var result = await sut.GetAsync(carrier, query, CancellationToken.None);

        Assert.NotNull(result);
    }
}