using CarrierRatesQueryV2.Core.Contracts.Rates;
using CarrierRatesQueryV2.Core.Interfaces.Rates;
using CarrierRatesQueryV2.Core.Rates.Clients;

namespace CarrierRatesQueryV2.Core.Rates.Adapters;

public sealed class UpsRateAdapter : ICarrierRateAdapter<MockUpsRateResponse>
{
    public ShippingRateQuote Adapt(MockUpsRateResponse source)
    {
        var options = source.Services.Select(x => new RateOption(
            ServiceName: x.Service,
            EstimatedDelivery: x.Eta,
            Price: new Money(x.Cost, x.Currency)))
            .ToList();

        return new ShippingRateQuote(source.Carrier, options);
    }
}
