using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Builder;
using CarrierRatesQueryV2.Data.Seeder;

namespace CarrierRatesQueryV2.Data;

public static class DependencyInjection
{
    public static IServiceCollection AddDataAccess(this IServiceCollection services)
    {
        services.AddDbContext<AppDbContext>(options =>
            options.UseInMemoryDatabase("CarrierRatesDb"));

        services.AddScoped<DataSeeder>();

        return services;
    }

    public static IApplicationBuilder SeedData(this IApplicationBuilder app)
    {
        using var scope = app.ApplicationServices.CreateScope();
        var seeder = scope.ServiceProvider.GetRequiredService<DataSeeder>();
        seeder.Seed();

        return app;
    }
}
