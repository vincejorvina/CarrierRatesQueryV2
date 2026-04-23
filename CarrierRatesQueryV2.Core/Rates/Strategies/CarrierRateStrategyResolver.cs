using CarrierRatesQueryV2.Core.Interfaces.Rates;

namespace CarrierRatesQueryV2.Core.Rates.Strategies;

public sealed class CarrierRateStrategyResolver(IEnumerable<ICarrierRateStrategy> strategies) : ICarrierRateStrategyResolver
{
    private readonly Dictionary<string, ICarrierRateStrategy> _strategies = strategies
        .GroupBy(x => x.CarrierSlug, StringComparer.OrdinalIgnoreCase)
        .ToDictionary(x => x.Key, x => x.First(), StringComparer.OrdinalIgnoreCase);

    public bool TryResolve(string carrierSlug, out ICarrierRateStrategy strategy)
    {
        strategy = default!;

        if (string.IsNullOrWhiteSpace(carrierSlug))
        {
            return false;
        }

        return _strategies.TryGetValue(carrierSlug.Trim(), out strategy!);
    }
}
