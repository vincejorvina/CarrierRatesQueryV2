using CarrierRatesQueryV2.Core.Contracts.Rates;
using CarrierRatesQueryV2.Core.Interfaces.Rates;
using CarrierRatesQueryV2.Core.Interfaces.Rates.Clients;
using CarrierRatesQueryV2.Core.Rates;
using CarrierRatesQueryV2.Core.Rates.Clients;

namespace CarrierRatesQueryV2.Core.Rates.Strategies;

public sealed class DhlRateStrategy(
    IMockDhlRatesClient mockDhlRatesClient,
    ICarrierRateAdapter<MockDhlRateResponse> dhlRateAdapter,
    IRateCache rateCache) : ICarrierRateStrategy
{
    public string CarrierSlug => "dhl";

    public async Task<ShippingRateQuote?> TryGetRatesAsync(CarrierContext carrier, RateQuery query, CancellationToken cancellationToken)
    {
        var cached = await rateCache.GetAsync(carrier, query, cancellationToken);
        if (cached is not null)
        {
            return cached;
        }

        var endpoint = carrier.Endpoints
            .FirstOrDefault(x => x.Operation.Equals("Rates", StringComparison.OrdinalIgnoreCase));

        if (endpoint is null)
        {
            return null;
        }

        var dhlRequest = new MockDhlRateRequest(
            new DhlFrom(
                query.Origin.PostalCode,
                query.Origin.CountryCode),
            new DhlTo(
                query.Destination.PostalCode,
                query.Destination.CountryCode),
            new DhlParcel(
                query.Package.Weight,
                new DhlSizeCm(
                    query.Package.Dimensions.Length,
                    query.Package.Dimensions.Width,
                    query.Package.Dimensions.Height)));

        var response = await mockDhlRatesClient.GetRatesAsync(endpoint.Endpoint, dhlRequest, cancellationToken);
        var quote = dhlRateAdapter.Adapt(response);

        await rateCache.SetAsync(carrier, query, quote, cancellationToken);

        return quote;
    }
}