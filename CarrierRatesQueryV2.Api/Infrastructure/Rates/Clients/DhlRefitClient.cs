using CarrierRatesQueryV2.Core.Interfaces.Rates.Clients;
using CarrierRatesQueryV2.Core.Rates.Clients;
using Refit;

namespace CarrierRatesQueryV2.Api.Infrastructure.Rates.Clients;

public interface IDhlRatesRefitApi
{
    [Post("")]
    Task<MockDhlRateResponse> GetRatesAsync([Body] MockDhlRateRequest request, CancellationToken cancellationToken);
}

public sealed class DhlRefitClient(IHttpClientFactory httpClientFactory) : IMockDhlRatesClient
{
    public async Task<MockDhlRateResponse> GetRatesAsync(string endpoint, MockDhlRateRequest request, CancellationToken cancellationToken)
    {
        if (!Uri.TryCreate(endpoint, UriKind.Absolute, out var endpointUri))
        {
            throw new ArgumentException("Endpoint must be a valid absolute URL.", nameof(endpoint));
        }

        var client = httpClientFactory.CreateClient(nameof(DhlRefitClient));
        client.BaseAddress = endpointUri;

        var api = RestService.For<IDhlRatesRefitApi>(client);
        return await api.GetRatesAsync(request, cancellationToken);
    }
}