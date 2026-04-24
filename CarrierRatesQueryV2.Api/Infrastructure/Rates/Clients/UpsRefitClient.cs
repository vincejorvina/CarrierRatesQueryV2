using CarrierRatesQueryV2.Core.Interfaces.Rates.Clients;
using CarrierRatesQueryV2.Core.Rates.Clients;
using Refit;

namespace CarrierRatesQueryV2.Api.Infrastructure.Rates.Clients;

public interface IUpsRatesRefitApi
{
    [Post("")]
    Task<MockUpsRateResponse> GetRatesAsync([Body] MockUpsRateRequest request, CancellationToken cancellationToken);
}

public sealed class UpsRefitClient(IHttpClientFactory httpClientFactory) : IMockUpsRatesClient
{
    public async Task<MockUpsRateResponse> GetRatesAsync(string endpoint, MockUpsRateRequest request, CancellationToken cancellationToken)
    {
        if (!Uri.TryCreate(endpoint, UriKind.Absolute, out var endpointUri))
        {
            throw new ArgumentException("Endpoint must be a valid absolute URL.", nameof(endpoint));
        }

        var client = httpClientFactory.CreateClient(nameof(UpsRefitClient));
        client.BaseAddress = endpointUri;

        var api = RestService.For<IUpsRatesRefitApi>(client);
        return await api.GetRatesAsync(request, cancellationToken);
    }
}