using CarrierRatesQueryV2.Core.Contracts.Rates;

namespace CarrierRatesQueryV2.Core.Interfaces.Rates;

public interface ICarrierRateStrategy
{
    string CarrierSlug { get; }
    Task<ShippingRateQuote?> TryGetRatesAsync(CarrierContext carrier, RateQuery query, CancellationToken cancellationToken);
}
