namespace CarrierRatesQueryV2.Core.Rates.Clients;

public sealed record MockFedExRateRequest(MockFedExOrigin Origin, MockFedExDestination Destination, MockFedExPackage Package);

public sealed record MockFedExOrigin(string PostalCode, string CountryCode);

public sealed record MockFedExDestination(string PostalCode, string CountryCode);

public sealed record MockFedExPackage(decimal Weight, MockFedExDimensions Dimensions);

public sealed record MockFedExDimensions(decimal Length, decimal Width, decimal Height);

public sealed record MockFedExRateResponse(string Carrier, IReadOnlyList<MockFedExServiceOption> ServiceOptions);

public sealed record MockFedExServiceOption(string ServiceName, string EstimatedDelivery, decimal Rate);
