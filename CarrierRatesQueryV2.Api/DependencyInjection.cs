using CarrierRatesQueryV2.Api.Services;
using CarrierRatesQueryV2.Data;

namespace CarrierRatesQueryV2.Api;

public static class DependencyInjection
{
    public static IServiceCollection AddServices(this IServiceCollection services)
    {
        services.SetupFastEndpoints();
        services.AddDataAccess();
        services.ResolveDependencies();
        return services;
    }

    static void ResolveDependencies(this IServiceCollection services)
    {
        // Add dependencies here
    }
}
