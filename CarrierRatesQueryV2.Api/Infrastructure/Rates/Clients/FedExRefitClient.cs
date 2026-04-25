using CarrierRatesQueryV2.Api.Services;
using CarrierRatesQueryV2.Core.Interfaces.Rates.Clients;
using CarrierRatesQueryV2.Core.Rates.Clients;
using Microsoft.Extensions.Logging;
using Refit;

namespace CarrierRatesQueryV2.Api.Infrastructure.Rates.Clients;

public interface IFedExRatesRefitApi
{
    [Post("")]
    Task<MockFedExRateResponse> GetRatesAsync([Body] MockFedExRateRequest request, CancellationToken cancellationToken);
}

public sealed class FedExRefitClient(
    IHttpClientFactory httpClientFactory,
    ILogger<FedExRefitClient> logger,
    ICarrierFailureTracker failureTracker) : IMockFedExRatesClient
{
    private readonly string _clientName = nameof(FedExRefitClient);

    public async Task<MockFedExRateResponse> GetRatesAsync(string endpoint, MockFedExRateRequest request, CancellationToken cancellationToken)
    {
        if (!Uri.TryCreate(endpoint, UriKind.Absolute, out var endpointUri))
        {
            throw new ArgumentException("Endpoint must be a valid absolute URL.", nameof(endpoint));
        }

        if (failureTracker.IsCarrierFailing("fedex"))
        {
            logger.LogWarning("FedEx carrier is currently failing, skipping request");
            throw new InvalidOperationException("FedEx carrier is temporarily unavailable");
        }

        try
        {
            var client = httpClientFactory.CreateClient(_clientName);
            client.BaseAddress = endpointUri;

            var api = RestService.For<IFedExRatesRefitApi>(client);
            var response = await api.GetRatesAsync(request, cancellationToken);

            failureTracker.RecordSuccess("fedex");
            logger.LogInformation("FedEx rates retrieved successfully");

            return response;
        }
        catch (Exception ex) when (ex is not InvalidOperationException)
        {
            failureTracker.RecordFailure("fedex");
            logger.LogError(ex, "Failed to retrieve FedEx rates after retries");
            throw;
        }
    }
}