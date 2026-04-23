using CarrierRatesQueryV2.Core.Contracts.Rates;

namespace CarrierRatesQueryV2.Core.Interfaces.Rates;

public interface ICarrierRateAdapter<in TCarrierResponse>
{
    ShippingRateQuote Adapt(TCarrierResponse source);
}
