using CarrierRatesQueryV2.Api.Services;
using Microsoft.Extensions.Caching.Memory;
using Xunit;

namespace CarrierRatesQueryV2.Tests.Unit;

public class CarrierFailureTrackerTests
{
    [Fact]
    public void IsCarrierFailing_NewCarrier_ReturnsFalse()
    {
        var cache = new MemoryCache(new MemoryCacheOptions());
        var tracker = new CarrierFailureTracker(cache);

        var result = tracker.IsCarrierFailing("fedex");

        Assert.False(result);
    }

    [Fact]
    public void RecordFailure_ThenIsCarrierFailing_ReturnsTrue()
    {
        var cache = new MemoryCache(new MemoryCacheOptions());
        var tracker = new CarrierFailureTracker(cache);

        tracker.RecordFailure("fedex");

        Assert.True(tracker.IsCarrierFailing("fedex"));
    }

    [Fact]
    public void RecordSuccess_AfterFailure_IsCarrierFailing_ReturnsFalse()
    {
        var cache = new MemoryCache(new MemoryCacheOptions());
        var tracker = new CarrierFailureTracker(cache);

        tracker.RecordFailure("fedex");
        tracker.RecordSuccess("fedex");

        Assert.False(tracker.IsCarrierFailing("fedex"));
    }

    [Fact]
    public void IsCarrierFailing_UnknownCarrier_ReturnsFalse()
    {
        var cache = new MemoryCache(new MemoryCacheOptions());
        var tracker = new CarrierFailureTracker(cache);

        tracker.RecordFailure("fedex");

        Assert.False(tracker.IsCarrierFailing("ups"));
    }
}