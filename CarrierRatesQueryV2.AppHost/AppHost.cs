var builder = DistributedApplication.CreateBuilder(args);

var mockFedEx = builder.AddProject<Projects.CarrierRatesQueryV2_MockFedEx>("carrierratesqueryv2-mockfedex");
var mockUps = builder.AddProject<Projects.CarrierRatesQueryV2_MockUps>("carrierratesqueryv2-mockups");
var mockDhl = builder.AddProject<Projects.CarrierRatesQueryV2_MockDhl>("carrierratesqueryv2-mockdhl");

builder.AddProject<Projects.CarrierRatesQueryV2_Api>("carrierratesqueryv2-api")
    .WithReference(mockFedEx)
    .WithReference(mockUps)
    .WithReference(mockDhl)
    .WaitFor(mockFedEx)
    .WaitFor(mockUps)
    .WaitFor(mockDhl);

await builder.Build().RunAsync();
