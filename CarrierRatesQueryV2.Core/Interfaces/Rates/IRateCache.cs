using CarrierRatesQueryV2.Core.Contracts.Rates;

namespace CarrierRatesQueryV2.Core.Rates;

public interface IRateCache
{
    Task<ShippingRateQuote?> GetAsync(CarrierContext carrier, RateQuery query, CancellationToken cancellationToken);
    Task SetAsync(CarrierContext carrier, RateQuery query, ShippingRateQuote quote, CancellationToken cancellationToken);
}