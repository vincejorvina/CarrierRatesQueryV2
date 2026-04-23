using CarrierRatesQueryV2.Api;
using CarrierRatesQueryV2.Data;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.Services.AddServices();

var app = builder.Build();

app.SeedData();

app.UseHttpsRedirection();

await app.RunAsync();