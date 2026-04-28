using CarrierRatesQueryV2.Core.Interfaces.Rates.Clients;
using CarrierRatesQueryV2.Core.Rates.Clients;

namespace CarrierRatesQueryV2.Tests.Infrastructure;

public sealed class TestFedExRatesClient : IMockFedExRatesClient
{
    public Task<MockFedExRateResponse> GetRatesAsync(string endpoint, MockFedExRateRequest request, CancellationToken cancellationToken)
    {
        var response = new MockFedExRateResponse(
            "FedEx",
            [new MockFedExServiceOption("FedEx Ground", DateTime.UtcNow.AddDays(3).ToString("O"), 12.34m)]);

        return Task.FromResult(response);
    }
}

public sealed class TestDhlRatesClient : IMockDhlRatesClient
{
    public Task<MockDhlRateResponse> GetRatesAsync(string endpoint, MockDhlRateRequest request, CancellationToken cancellationToken)
    {
        var response = new MockDhlRateResponse(
            "DHL",
            [new MockDhlServiceOption("DHL Express", DateTime.UtcNow.AddDays(2), 15.67m, "USD")]);

        return Task.FromResult(response);
    }
}

public sealed class TestUpsRatesClient : IMockUpsRatesClient
{
    public Task<MockUpsRateResponse> GetRatesAsync(string endpoint, MockUpsRateRequest request, CancellationToken cancellationToken)
    {
        var response = new MockUpsRateResponse(
            "UPS",
            [new MockUpsServiceOption("UPS Ground", DateTime.UtcNow.AddDays(4), 10.50m, "USD")]);

        return Task.FromResult(response);
    }
}
