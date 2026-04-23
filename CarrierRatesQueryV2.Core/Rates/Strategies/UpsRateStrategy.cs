using CarrierRatesQueryV2.Core.Contracts.Rates;
using CarrierRatesQueryV2.Core.Interfaces.Rates;
using CarrierRatesQueryV2.Core.Interfaces.Rates.Clients;
using CarrierRatesQueryV2.Core.Rates.Clients;

namespace CarrierRatesQueryV2.Core.Rates.Strategies;

public sealed class UpsRateStrategy(
    IMockUpsRatesClient mockUpsRatesClient,
    ICarrierRateAdapter<MockUpsRateResponse> upsRateAdapter) : ICarrierRateStrategy
{
    public string CarrierSlug => "ups";

    public async Task<ShippingRateQuote?> TryGetRatesAsync(CarrierContext carrier, RateQuery query, CancellationToken cancellationToken)
    {
        var endpoint = carrier.Endpoints
            .FirstOrDefault(x => x.Operation.Equals("Rates", StringComparison.OrdinalIgnoreCase));

        if (endpoint is null)
        {
            return null;
        }

        var upsRequest = new MockUpsRateRequest(
            query.Package.Weight,
            query.Package.Dimensions.Length,
            query.Package.Dimensions.Width,
            query.Package.Dimensions.Height);

        var response = await mockUpsRatesClient.GetRatesAsync(endpoint.Endpoint, upsRequest, cancellationToken);
        return upsRateAdapter.Adapt(response);
    }
}
