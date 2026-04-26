namespace CarrierRatesQueryV2.Core.Rates.Clients;

public sealed record MockUpsRateRequest(UpsShipment Shipment);

public sealed record UpsShipment(
    string OriginPostalCode,
    string DestinationPostalCode,
    string OriginCountryCode,
    string DestinationCountryCode,
    decimal WeightLbs,
    UpsDimensionsInches DimensionsInches);

public sealed record UpsDimensionsInches(decimal Length, decimal Width, decimal Height);

public sealed record MockUpsRateResponse(string Carrier, IReadOnlyList<MockUpsServiceOption> Services);

public sealed record MockUpsServiceOption(string Service, DateTime Eta, decimal Cost, string Currency);
