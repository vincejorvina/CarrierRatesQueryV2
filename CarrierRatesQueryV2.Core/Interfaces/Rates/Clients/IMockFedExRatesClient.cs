using CarrierRatesQueryV2.Core.Rates.Clients;

namespace CarrierRatesQueryV2.Core.Interfaces.Rates.Clients;

public interface IMockFedExRatesClient
{
    Task<MockFedExRateResponse> GetRatesAsync(string endpoint, MockFedExRateRequest request, CancellationToken cancellationToken);
}
