using CarrierRatesQueryV2.Core.Interfaces.Rates;
using CarrierRatesQueryV2.Core.Rates;
using CarrierRatesQueryV2.Core.Rates.Adapters;
using CarrierRatesQueryV2.Core.Rates.Clients;
using CarrierRatesQueryV2.Core.Rates.Strategies;
using Microsoft.Extensions.DependencyInjection;

namespace CarrierRatesQueryV2.Core;

public static class DependencyInjection
{
    public static IServiceCollection AddCoreServices(this IServiceCollection services)
    {
        services.AddScoped<ICarrierRateStrategyResolver, CarrierRateStrategyResolver>();

        services.AddScoped<ICarrierRateStrategy, FedExRateStrategy>();
        services.AddScoped<ICarrierRateStrategy, DhlRateStrategy>();
        services.AddScoped<ICarrierRateStrategy, UpsRateStrategy>();

        services.AddScoped<ICarrierRateAdapter<MockFedExRateResponse>, FedExRateAdapter>();
        services.AddScoped<ICarrierRateAdapter<MockDhlRateResponse>, DhlRateAdapter>();
        services.AddScoped<ICarrierRateAdapter<MockUpsRateResponse>, UpsRateAdapter>();

        services.AddScoped<IRateCache, MemoryRateCache>();

        return services;
    }
}
