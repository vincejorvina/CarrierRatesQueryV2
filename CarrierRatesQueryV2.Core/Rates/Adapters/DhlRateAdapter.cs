using CarrierRatesQueryV2.Core.Contracts.Rates;
using CarrierRatesQueryV2.Core.Interfaces.Rates;
using CarrierRatesQueryV2.Core.Rates.Clients;

namespace CarrierRatesQueryV2.Core.Rates.Adapters;

public sealed class DhlRateAdapter : ICarrierRateAdapter<MockDhlRateResponse>
{
    public ShippingRateQuote Adapt(MockDhlRateResponse source)
    {
        var options = source.Options.Select(x => new RateOption(
            ServiceName: x.Product,
            EstimatedDelivery: x.DeliveryDate,
            Price: new Money(x.Price, x.Currency)))
            .ToList();

        return new ShippingRateQuote(source.Carrier, options);
    }
}
