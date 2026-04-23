using CarrierRatesQueryV2.Core.Contracts.Rates;
using CarrierRatesQueryV2.Core.Interfaces.Rates;
using CarrierRatesQueryV2.Core.Interfaces.Rates.Clients;
using CarrierRatesQueryV2.Core.Rates.Clients;

namespace CarrierRatesQueryV2.Core.Rates.Strategies;

public sealed class FedExRateStrategy(
    IMockFedExRatesClient mockFedExRatesClient,
    ICarrierRateAdapter<MockFedExRateResponse> fedExRateAdapter) : ICarrierRateStrategy
{
    public string CarrierSlug => "fedex";

    public async Task<ShippingRateQuote?> TryGetRatesAsync(CarrierContext carrier, RateQuery query, CancellationToken cancellationToken)
    {
        var endpoint = carrier.Endpoints
            .FirstOrDefault(x => x.Operation.Equals("Rates", StringComparison.OrdinalIgnoreCase));

        if (endpoint is null)
        {
            return null;
        }

        var fedExRequest = new MockFedExRateRequest(
            new MockFedExPackage(
                query.Package.Weight,
                new MockFedExDimensions(
                    query.Package.Dimensions.Length,
                    query.Package.Dimensions.Width,
                    query.Package.Dimensions.Height)));

        var response = await mockFedExRatesClient.GetRatesAsync(endpoint.Endpoint, fedExRequest, cancellationToken);
        return fedExRateAdapter.Adapt(response);
    }
}
