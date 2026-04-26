namespace CarrierRatesQueryV2.Core.Rates.Clients;

public sealed record MockDhlRateRequest(DhlFrom From, DhlTo To, DhlParcel Parcel);

public sealed record DhlFrom(string ZipCode, string Country);

public sealed record DhlTo(string ZipCode, string Country);

public sealed record DhlParcel(decimal WeightKg, DhlSizeCm SizeCm);

public sealed record DhlSizeCm(decimal Length, decimal Width, decimal Height);

public sealed record MockDhlRateResponse(string Carrier, IReadOnlyList<MockDhlServiceOption> Options);

public sealed record MockDhlServiceOption(string Product, DateTime DeliveryDate, decimal Price, string Currency);
