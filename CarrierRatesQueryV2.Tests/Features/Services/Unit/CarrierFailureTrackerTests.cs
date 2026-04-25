using CarrierRatesQueryV2.Api.Services;
using Microsoft.Extensions.Caching.Memory;
using Shouldly;
using Xunit;

namespace CarrierRatesQueryV2.Tests.Features.Services.Unit;

public class CarrierFailureTrackerTests
{
    [Fact]
    public void IsCarrierFailing_NewCarrier_ReturnsFalse()
    {
        var cache = new MemoryCache(new MemoryCacheOptions());
        var tracker = new CarrierFailureTracker(cache);

        var result = tracker.IsCarrierFailing("fedex");

        result.ShouldBeFalse();
    }

    [Fact]
    public void RecordFailure_ThenIsCarrierFailing_ReturnsTrue()
    {
        var cache = new MemoryCache(new MemoryCacheOptions());
        var tracker = new CarrierFailureTracker(cache);

        tracker.RecordFailure("fedex");

        tracker.IsCarrierFailing("fedex").ShouldBeTrue();
    }

    [Fact]
    public void RecordSuccess_AfterFailure_IsCarrierFailing_ReturnsFalse()
    {
        var cache = new MemoryCache(new MemoryCacheOptions());
        var tracker = new CarrierFailureTracker(cache);

        tracker.RecordFailure("fedex");
        tracker.RecordSuccess("fedex");

        tracker.IsCarrierFailing("fedex").ShouldBeFalse();
    }

    [Fact]
    public void IsCarrierFailing_UnknownCarrier_ReturnsFalse()
    {
        var cache = new MemoryCache(new MemoryCacheOptions());
        var tracker = new CarrierFailureTracker(cache);

        tracker.RecordFailure("fedex");

        tracker.IsCarrierFailing("ups").ShouldBeFalse();
    }
}