using CarrierRatesQueryV2.Core.Rates.Clients;

namespace CarrierRatesQueryV2.Core.Interfaces.Rates.Clients;

public interface IMockUpsRatesClient
{
    Task<MockUpsRateResponse> GetRatesAsync(string endpoint, MockUpsRateRequest request, CancellationToken cancellationToken);
}
