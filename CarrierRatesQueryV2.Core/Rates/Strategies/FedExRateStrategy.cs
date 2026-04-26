using CarrierRatesQueryV2.Core.Contracts.Rates;
using CarrierRatesQueryV2.Core.Interfaces.Rates;
using CarrierRatesQueryV2.Core.Interfaces.Rates.Clients;
using CarrierRatesQueryV2.Core.Rates;
using CarrierRatesQueryV2.Core.Rates.Clients;

namespace CarrierRatesQueryV2.Core.Rates.Strategies;

public sealed class FedExRateStrategy(
    IMockFedExRatesClient mockFedExRatesClient,
    ICarrierRateAdapter<MockFedExRateResponse> fedExRateAdapter,
    IRateCache rateCache) : ICarrierRateStrategy
{
    public string CarrierSlug => "fedex";

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

        var fedExRequest = new MockFedExRateRequest(
            new MockFedExOrigin(
                query.Origin.PostalCode,
                query.Origin.CountryCode),
            new MockFedExDestination(
                query.Destination.PostalCode,
                query.Destination.CountryCode),
            new MockFedExPackage(
                query.Package.Weight,
                new MockFedExDimensions(
                    query.Package.Dimensions.Length,
                    query.Package.Dimensions.Width,
                    query.Package.Dimensions.Height)));

        var response = await mockFedExRatesClient.GetRatesAsync(endpoint.Endpoint, fedExRequest, cancellationToken);
        var quote = fedExRateAdapter.Adapt(response);

        await rateCache.SetAsync(carrier, query, quote, cancellationToken);

        return quote;
    }
}