namespace CarrierRatesQueryV2.Core.Rates.Clients;

public sealed record MockDhlRateRequest(decimal Weight, decimal Length, decimal Width, decimal Height);

public sealed record MockDhlRateResponse(string Carrier, IReadOnlyList<MockDhlServiceOption> Options);

public sealed record MockDhlServiceOption(string Product, DateTime DeliveryDate, decimal Price, string Currency);
