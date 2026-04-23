namespace CarrierRatesQueryV2.Core.Rates.Clients;

public sealed record MockFedExRateRequest(MockFedExPackage Package);

public sealed record MockFedExPackage(decimal Weight, MockFedExDimensions Dimensions);

public sealed record MockFedExDimensions(decimal Length, decimal Width, decimal Height);

public sealed record MockFedExRateResponse(string Carrier, IReadOnlyList<MockFedExServiceOption> ServiceOptions);

public sealed record MockFedExServiceOption(string ServiceName, string EstimatedDelivery, decimal Rate);
