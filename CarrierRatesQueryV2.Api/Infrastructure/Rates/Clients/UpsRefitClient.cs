using CarrierRatesQueryV2.Api.Services;
using CarrierRatesQueryV2.Core.Interfaces.Rates.Clients;
using CarrierRatesQueryV2.Core.Rates.Clients;
using Microsoft.Extensions.Logging;
using Refit;

namespace CarrierRatesQueryV2.Api.Infrastructure.Rates.Clients;

public interface IUpsRatesRefitApi
{
    [Post("")]
    Task<MockUpsRateResponse> GetRatesAsync([Body] MockUpsRateRequest request, CancellationToken cancellationToken);
}

public sealed class UpsRefitClient(
    IHttpClientFactory httpClientFactory,
    ILogger<UpsRefitClient> logger,
    ICarrierFailureTracker failureTracker) : IMockUpsRatesClient
{
    private readonly string _clientName = nameof(UpsRefitClient);

    public async Task<MockUpsRateResponse> GetRatesAsync(string endpoint, MockUpsRateRequest request, CancellationToken cancellationToken)
    {
        if (!Uri.TryCreate(endpoint, UriKind.Absolute, out var endpointUri))
        {
            throw new ArgumentException("Endpoint must be a valid absolute URL.", nameof(endpoint));
        }

        if (failureTracker.IsCarrierFailing("ups"))
        {
            logger.LogWarning("UPS carrier is currently failing, skipping request");
            throw new InvalidOperationException("UPS carrier is temporarily unavailable");
        }

        try
        {
            var client = httpClientFactory.CreateClient(_clientName);
            client.BaseAddress = endpointUri;

            var api = RestService.For<IUpsRatesRefitApi>(client);
            var response = await api.GetRatesAsync(request, cancellationToken);

            failureTracker.RecordSuccess("ups");
            logger.LogInformation("UPS rates retrieved successfully");

            return response;
        }
        catch (Exception ex) when (ex is not InvalidOperationException)
        {
            failureTracker.RecordFailure("ups");
            logger.LogError(ex, "Failed to retrieve UPS rates after retries");
            throw;
        }
    }
}