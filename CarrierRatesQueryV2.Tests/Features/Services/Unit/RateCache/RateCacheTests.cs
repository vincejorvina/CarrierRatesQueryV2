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
}