using CarrierRatesQueryV2.Core.Interfaces.Rates.Clients;
using CarrierRatesQueryV2.Core.Rates.Clients;
using Refit;

namespace CarrierRatesQueryV2.Api.Infrastructure.Rates.Clients;

public interface IFedExRatesRefitApi
{
    [Post("")]
    Task<MockFedExRateResponse> GetRatesAsync([Body] MockFedExRateRequest request, CancellationToken cancellationToken);
}

public sealed class FedExRefitClient(IHttpClientFactory httpClientFactory) : IMockFedExRatesClient
{
    public async Task<MockFedExRateResponse> GetRatesAsync(string endpoint, MockFedExRateRequest request, CancellationToken cancellationToken)
    {
        if (!Uri.TryCreate(endpoint, UriKind.Absolute, out var endpointUri))
        {
            throw new ArgumentException("Endpoint must be a valid absolute URL.", nameof(endpoint));
        }

        var client = httpClientFactory.CreateClient(nameof(FedExRefitClient));
        client.BaseAddress = endpointUri;

        var api = RestService.For<IFedExRatesRefitApi>(client);
        return await api.GetRatesAsync(request, cancellationToken);
    }
}
