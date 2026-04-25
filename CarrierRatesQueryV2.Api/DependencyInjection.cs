using CarrierRatesQueryV2.Api.Services;
using CarrierRatesQueryV2.Api.Infrastructure;
using CarrierRatesQueryV2.Api.Infrastructure.Rates.Clients;
using CarrierRatesQueryV2.Core;
using CarrierRatesQueryV2.Core.Interfaces.Rates.Clients;
using CarrierRatesQueryV2.Data;
using Microsoft.Extensions.Http.Resilience;

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

        services.AddCarrierHttpClient(nameof(FedExRefitClient));
        services.AddCarrierHttpClient(nameof(DhlRefitClient));
        services.AddCarrierHttpClient(nameof(UpsRefitClient));

        services.AddScoped<IMockFedExRatesClient, FedExRefitClient>();
        services.AddScoped<IMockDhlRatesClient, DhlRefitClient>();
        services.AddScoped<IMockUpsRatesClient, UpsRefitClient>();

        services.AddScoped<IRequestRoleAccessor, RequestRoleAccessor>();
        services.AddScoped<ICarrierFailureTracker, CarrierFailureTracker>();

        services.AddScoped<ICarrierManagementService, CarrierManagementService>();
    }

    public static Microsoft.Extensions.Http.Resilience.IHttpStandardResiliencePipelineBuilder AddCarrierHttpClient(this IServiceCollection services, string name)
    {
        return services.AddHttpClient(name)
            .AddStandardResilienceHandler(options =>
            {
                options.Retry.MaxRetryAttempts = 3;
                options.Retry.Delay = TimeSpan.FromSeconds(2);
                options.Retry.UseJitter = true;

                options.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(30);
                options.CircuitBreaker.FailureRatio = 0.5;
                options.CircuitBreaker.MinimumThroughput = 5;
                options.CircuitBreaker.BreakDuration = TimeSpan.FromSeconds(30);
            });
    }
}