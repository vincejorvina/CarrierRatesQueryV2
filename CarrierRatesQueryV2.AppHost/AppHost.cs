var builder = DistributedApplication.CreateBuilder(args);

var mockFedEx = builder.AddProject<Projects.CarrierRatesQueryV2_MockFedEx>("carrierratesqueryv2-mockfedex");

builder.AddProject<Projects.CarrierRatesQueryV2_Api>("carrierratesqueryv2-api")
    .WithReference(mockFedEx)
    .WaitFor(mockFedEx);

await builder.Build().RunAsync();
