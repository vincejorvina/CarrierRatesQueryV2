namespace CarrierRatesQueryV2.Core.Contracts.Rates;

public sealed record Location(string PostalCode, string CountryCode);

public sealed record PackageDimensions(decimal Length, decimal Width, decimal Height);

public sealed record Package(decimal Weight, PackageDimensions Dimensions);

public sealed record RateQuery(Location Origin, Location Destination, Package Package);

public sealed record CarrierEndpointConfig(string Operation, string Endpoint);

public sealed record CarrierContext(
    Guid Id,
    string Name,
    string Slug,
    DateTime? UpdatedAtUtc,
    IReadOnlyList<CarrierEndpointConfig> Endpoints);

public sealed record Money(decimal Amount, string Currency);

public sealed record RateOption(string ServiceName, DateTime EstimatedDelivery, Money Price);

public sealed record ShippingRateQuote(string Carrier, IReadOnlyList<RateOption> RateOptions);
