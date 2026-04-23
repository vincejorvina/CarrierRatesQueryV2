namespace CarrierRatesQueryV2.Core.Interfaces.Rates;

public interface ICarrierRateStrategyResolver
{
    bool TryResolve(string carrierSlug, out ICarrierRateStrategy strategy);
}
