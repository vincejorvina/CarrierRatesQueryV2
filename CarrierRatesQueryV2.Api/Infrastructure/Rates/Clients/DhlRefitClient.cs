using CarrierRatesQueryV2.Api.Services;
using CarrierRatesQueryV2.Core.Interfaces.Rates.Clients;
using CarrierRatesQueryV2.Core.Rates.Clients;
using Microsoft.Extensions.Logging;
using Refit;

namespace CarrierRatesQueryV2.Api.Infrastructure.Rates.Clients;

public interface IDhlRatesRefitApi
{
    [Post("")]
    Task<MockDhlRateResponse> GetRatesAsync([Body] MockDhlRateRequest request, CancellationToken cancellationToken);
}

public sealed class DhlRefitClient(
    IHttpClientFactory httpClientFactory,
    ILogger<DhlRefitClient> logger,
    ICarrierFailureTracker failureTracker) : IMockDhlRatesClient
{
    private readonly string _clientName = nameof(DhlRefitClient);

    public async Task<MockDhlRateResponse> GetRatesAsync(string endpoint, MockDhlRateRequest request, CancellationToken cancellationToken)
    {
        if (!Uri.TryCreate(endpoint, UriKind.Absolute, out var endpointUri))
        {
            throw new ArgumentException("Endpoint must be a valid absolute URL.", nameof(endpoint));
        }

        if (failureTracker.IsCarrierFailing("dhl"))
        {
            logger.LogWarning("DHL carrier is currently failing, skipping request");
            throw new InvalidOperationException("DHL carrier is temporarily unavailable");
        }

        try
        {
            var client = httpClientFactory.CreateClient(_clientName);
            client.BaseAddress = endpointUri;

            var api = RestService.For<IDhlRatesRefitApi>(client);
            var response = await api.GetRatesAsync(request, cancellationToken);

            failureTracker.RecordSuccess("dhl");
            logger.LogInformation("DHL rates retrieved successfully");

            return response;
        }
        catch (Exception ex) when (ex is not InvalidOperationException)
        {
            failureTracker.RecordFailure("dhl");
            logger.LogError(ex, "Failed to retrieve DHL rates after retries");
            throw;
        }
    }
}