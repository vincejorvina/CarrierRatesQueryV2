var builder = DistributedApplication.CreateBuilder(args);

builder.AddProject<Projects.CarrierRatesQueryV2_Api>("carrierratesqueryv2-api");

await builder.Build().RunAsync();
