using CarrierRatesQueryV2.Data;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace CarrierRatesQueryV2.Api;

public static class DependencyInjection
{
    public static IServiceCollection AddServices(this IServiceCollection services)
    {
        services.AddDataAccess();
        services.ResolveDependencies();
        return services;
    }

    static void ResolveDependencies(this IServiceCollection services)
    {
        // Add dependencies here
    }
}
