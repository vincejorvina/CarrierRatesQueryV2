using CarrierRatesQueryV2.Core.Rates.Clients;

namespace CarrierRatesQueryV2.Core.Interfaces.Rates.Clients;

public interface IMockDhlRatesClient
{
    Task<MockDhlRateResponse> GetRatesAsync(string endpoint, MockDhlRateRequest request, CancellationToken cancellationToken);
}
