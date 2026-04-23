var builder = DistributedApplication.CreateBuilder(args);

builder.AddProject<Projects.CarrierRatesQueryV2>("carrierratesqueryv2");

builder.Build().Run();
