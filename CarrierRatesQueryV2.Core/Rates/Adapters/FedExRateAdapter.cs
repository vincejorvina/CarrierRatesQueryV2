using CarrierRatesQueryV2.Core.Contracts.Rates;
using CarrierRatesQueryV2.Core.Interfaces.Rates;
using CarrierRatesQueryV2.Core.Rates.Clients;

namespace CarrierRatesQueryV2.Core.Rates.Adapters;

public sealed class FedExRateAdapter : ICarrierRateAdapter<MockFedExRateResponse>
{
    public ShippingRateQuote Adapt(MockFedExRateResponse source)
    {
        var options = source.ServiceOptions.Select(x => new RateOption(
            ServiceName: x.ServiceName,
            EstimatedDelivery: DateTime.Parse(x.EstimatedDelivery, System.Globalization.CultureInfo.InvariantCulture),
            Price: new Money(x.Rate, "USD")))
            .ToList();

        return new ShippingRateQuote(source.Carrier, options);
    }
}
