using CarrierRatesQueryV2.Core.Rates.Clients;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapDefaultEndpoints();

app.UseHttpsRedirection();

app.MapPost("/api/ups/shipping-rates", ([FromBody] MockUpsRateRequest request) =>
{
    var shipment = request.Shipment;
    var weightMultiplier = Math.Max(shipment.WeightLbs, 1m) * 0.18m;
    var dims = shipment.DimensionsInches;
    var volume = Math.Max(dims.Length * dims.Width * dims.Height, 1m);
    var dimensionMultiplier = volume * 0.0025m;
    var packageSurcharge = weightMultiplier + dimensionMultiplier;

    var response = new MockUpsRateResponse(
        Carrier: "UPS",
        Services:
        [
            new MockUpsServiceOption(
                Service: "UPS Ground",
                Eta: DateTime.UtcNow.AddDays(5),
                Cost: Math.Round(15.20m + packageSurcharge, 2),
                Currency: "USD"
            ),
            new MockUpsServiceOption(
                Service: "UPS 2nd Day Air",
                Eta: DateTime.UtcNow.AddDays(2),
                Cost: Math.Round(28.45m + packageSurcharge, 2),
                Currency: "USD"
            ),
            new MockUpsServiceOption(
                Service: "UPS Next Day Air",
                Eta: DateTime.UtcNow.AddDays(1),
                Cost: Math.Round(52.99m + packageSurcharge, 2),
                Currency: "USD"
            )
        ]);

    return Results.Ok(response);
})
.WithName("PostUpsRates")
.Accepts<MockUpsRateRequest>("application/json")
.Produces<MockUpsRateResponse>(StatusCodes.Status200OK)
.WithOpenApi();

await app.RunAsync();

public sealed record MockUpsRateRequest(UpsShipment Shipment);

public sealed record UpsShipment(
    string OriginPostalCode,
    string DestinationPostalCode,
    string OriginCountryCode,
    string DestinationCountryCode,
    decimal WeightLbs,
    UpsDimensionsInches DimensionsInches);

public sealed record UpsDimensionsInches(decimal Length, decimal Width, decimal Height);