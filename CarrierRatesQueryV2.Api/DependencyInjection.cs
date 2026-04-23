using CarrierRatesQueryV2.Api.Services;
using CarrierRatesQueryV2.Api.Infrastructure.Rates.Clients;
using CarrierRatesQueryV2.Core;
using CarrierRatesQueryV2.Core.Interfaces.Rates.Clients;
using CarrierRatesQueryV2.Data;

namespace CarrierRatesQueryV2.Api;

public static class DependencyInjection
{
    public static IServiceCollection AddServices(this IServiceCollection services)
    {
        services.AddProblemDetails();
        services.AddMemoryCache();
        services.AddHttpContextAccessor();

        services.SetupFastEndpoints();

        services.AddDataAccess();

        services.ResolveDependencies();

        return services;
    }

static void ResolveDependencies(this IServiceCollection services)
    {
        services.AddCoreServices();

        services.AddHttpClient();
        services.AddScoped<IMockFedExRatesClient, FedExRefitClient>();
    }
}
