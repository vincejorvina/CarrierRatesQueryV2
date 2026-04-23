using CarrierRatesQueryV2.Core.Contracts.Rates;
using Microsoft.Extensions.Caching.Memory;

namespace CarrierRatesQueryV2.Core.Rates;

public sealed class MemoryRateCache : IRateCache
{
    private readonly IMemoryCache _cache;
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(2);

    public MemoryRateCache(IMemoryCache cache)
    {
        _cache = cache;
    }

    public Task<ShippingRateQuote?> GetAsync(CarrierContext carrier, RateQuery query, CancellationToken cancellationToken)
    {
        var key = BuildCacheKey(carrier, query);
        
        if (_cache.TryGetValue(key, out ShippingRateQuote? cached))
        {
            return Task.FromResult(cached);
        }

        return Task.FromResult<ShippingRateQuote?>(null);
    }

    public Task SetAsync(CarrierContext carrier, RateQuery query, ShippingRateQuote quote, CancellationToken cancellationToken)
    {
        var key = BuildCacheKey(carrier, query);
        _cache.Set(key, quote, CacheDuration);
        return Task.CompletedTask;
    }

    private static string BuildCacheKey(CarrierContext carrier, RateQuery query)
    {
        var ratesEndpoint = carrier.Endpoints
            .FirstOrDefault(x => x.Operation.Equals("Rates", StringComparison.OrdinalIgnoreCase))?
            .Endpoint ?? "none";

        var updatedAt = carrier.UpdatedAtUtc?.Ticks ?? 0;

        return string.Create(
            System.Globalization.CultureInfo.InvariantCulture,
            $"rates:{carrier.Slug}:{carrier.Id:N}:{updatedAt}:{ratesEndpoint}:{query.Package.Weight:F3}:{query.Package.Dimensions.Length:F3}:{query.Package.Dimensions.Width:F3}:{query.Package.Dimensions.Height:F3}");
    }
}