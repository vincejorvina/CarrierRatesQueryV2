namespace CarrierRatesQueryV2.Core.Rates.Clients;

public sealed record MockUpsRateRequest(decimal Weight, decimal Length, decimal Width, decimal Height);

public sealed record MockUpsRateResponse(string Carrier, IReadOnlyList<MockUpsServiceOption> Services);

public sealed record MockUpsServiceOption(string Service, DateTime Eta, decimal Cost, string Currency);
