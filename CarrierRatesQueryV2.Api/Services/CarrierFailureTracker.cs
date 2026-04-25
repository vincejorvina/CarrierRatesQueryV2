using Microsoft.Extensions.Caching.Memory;

namespace CarrierRatesQueryV2.Api.Services;

public interface ICarrierFailureTracker
{
    bool IsCarrierFailing(string carrierSlug);
    void RecordFailure(string carrierSlug);
    void RecordSuccess(string carrierSlug);
}

public sealed class CarrierFailureTracker : ICarrierFailureTracker
{
    private readonly IMemoryCache _cache;
    private static readonly TimeSpan FailureCacheDuration = TimeSpan.FromSeconds(30);

    public CarrierFailureTracker(IMemoryCache cache)
    {
        _cache = cache;
    }

    public bool IsCarrierFailing(string carrierSlug)
    {
        return _cache.TryGetValue($"carrier-fail:{carrierSlug}", out _);
    }

    public void RecordFailure(string carrierSlug)
    {
        _cache.Set($"carrier-fail:{carrierSlug}", true, FailureCacheDuration);
    }

    public void RecordSuccess(string carrierSlug)
    {
        _cache.Remove($"carrier-fail:{carrierSlug}");
    }
}