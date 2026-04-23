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

app.MapPost("/api/dhl/rates", ([FromBody] MockDhlRateRequest request) =>
{
    var weightMultiplier = Math.Max(request.Weight, 1m) * 0.20m;
    var volume = Math.Max(request.Length * request.Width * request.Height, 1m);
    var dimensionMultiplier = volume * 0.003m;
    var packageSurcharge = weightMultiplier + dimensionMultiplier;

    var response = new MockDhlRateResponse(
        Carrier: "DHL",
        Options:
        [
            new MockDhlServiceOption(
                Product: "DHL Economy Select",
                DeliveryDate: DateTime.UtcNow.AddDays(6),
                Price: Math.Round(11.00m + packageSurcharge, 2),
                Currency: "USD"
            ),
            new MockDhlServiceOption(
                Product: "DHL Express",
                DeliveryDate: DateTime.UtcNow.AddDays(2),
                Price: Math.Round(32.50m + packageSurcharge, 2),
                Currency: "USD"
            ),
            new MockDhlServiceOption(
                Product: "DHL Express Plus",
                DeliveryDate: DateTime.UtcNow.AddDays(1),
                Price: Math.Round(55.75m + packageSurcharge, 2),
                Currency: "USD"
            )
        ]);

    return Results.Ok(response);
})
.WithName("PostDhlRates")
.Accepts<MockDhlRateRequest>("application/json")
.Produces<MockDhlRateResponse>(StatusCodes.Status200OK)
.WithOpenApi();

await app.RunAsync();