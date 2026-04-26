using CarrierRatesQueryV2.Core;
using CarrierRatesQueryV2.Core.Contracts.Rates;
using CarrierRatesQueryV2.Core.Interfaces.Rates;
using CarrierRatesQueryV2.Core.Rates.Strategies;
using Shouldly;
using Xunit;

namespace CarrierRatesQueryV2.Tests.Features.Services.Unit;

public class CarrierRateStrategyResolverTests
{
    private class TestStrategy : ICarrierRateStrategy
    {
        public string CarrierSlug => "testcarrier";
        public Task<ShippingRateQuote?> TryGetRatesAsync(CarrierContext carrier, RateQuery query, CancellationToken cancellationToken) => Task.FromResult<ShippingRateQuote?>(null);
    }

    private class FedExStrategy : ICarrierRateStrategy
    {
        public string CarrierSlug => "fedex";
        public Task<ShippingRateQuote?> TryGetRatesAsync(CarrierContext carrier, RateQuery query, CancellationToken cancellationToken) => Task.FromResult<ShippingRateQuote?>(null);
    }

    private class UpsStrategy : ICarrierRateStrategy
    {
        public string CarrierSlug => "ups";
        public Task<ShippingRateQuote?> TryGetRatesAsync(CarrierContext carrier, RateQuery query, CancellationToken cancellationToken) => Task.FromResult<ShippingRateQuote?>(null);
    }

    [Fact]
    public void Constructor_WithValidStrategies_ResolvesCorrectly()
    {
        var strategies = new List<ICarrierRateStrategy> { new FedExStrategy(), new UpsStrategy() };
        var resolver = new CarrierRateStrategyResolver(strategies);

        resolver.TryResolve("fedex", out var strategy).ShouldBeTrue();
        strategy.CarrierSlug.ShouldBe("fedex");
    }

    [Fact]
    public void TryResolve_CaseInsensitive_ReturnsStrategy()
    {
        var strategies = new List<ICarrierRateStrategy> { new FedExStrategy() };
        var resolver = new CarrierRateStrategyResolver(strategies);

        resolver.TryResolve("FEDEX", out var strategy).ShouldBeTrue();
        strategy.CarrierSlug.ShouldBe("fedex");
    }

    [Fact]
    public void TryResolve_UnknownCarrier_ReturnsFalse()
    {
        var strategies = new List<ICarrierRateStrategy> { new FedExStrategy() };
        var resolver = new CarrierRateStrategyResolver(strategies);

        var result = resolver.TryResolve("unknown", out var strategy);

        result.ShouldBeFalse();
        strategy.ShouldBe(default(ICarrierRateStrategy));
    }

    [Fact]
    public void TryResolve_EmptyString_ReturnsFalse()
    {
        var strategies = new List<ICarrierRateStrategy> { new FedExStrategy() };
        var resolver = new CarrierRateStrategyResolver(strategies);

        var result = resolver.TryResolve("", out var strategy);

        result.ShouldBeFalse();
    }

    [Fact]
    public void TryResolve_NullString_ReturnsFalse()
    {
        var strategies = new List<ICarrierRateStrategy> { new FedExStrategy() };
        var resolver = new CarrierRateStrategyResolver(strategies);

        var result = resolver.TryResolve(null!, out var strategy);

        result.ShouldBeFalse();
    }

    [Fact]
    public void TryResolve_WhitespaceString_ReturnsFalse()
    {
        var strategies = new List<ICarrierRateStrategy> { new FedExStrategy() };
        var resolver = new CarrierRateStrategyResolver(strategies);

        var result = resolver.TryResolve("   ", out var strategy);

        result.ShouldBeFalse();
    }

    [Fact]
    public void TryResolve_TrimmedSlug_MatchesStrategy()
    {
        var strategies = new List<ICarrierRateStrategy> { new FedExStrategy() };
        var resolver = new CarrierRateStrategyResolver(strategies);

        resolver.TryResolve("  fedex  ", out var strategy).ShouldBeTrue();
    }

    [Fact]
    public void Constructor_DuplicateSlugs_UsesFirstOne()
    {
        var strategies = new List<ICarrierRateStrategy>
        {
            new FedExStrategy(),
            new TestStrategy()
        };
        var resolver = new CarrierRateStrategyResolver(strategies);

        resolver.TryResolve("fedex", out var strategy).ShouldBeTrue();
    }
}