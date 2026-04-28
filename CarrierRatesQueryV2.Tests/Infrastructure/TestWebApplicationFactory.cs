using CarrierRatesQueryV2.Api;
using CarrierRatesQueryV2.Core.Interfaces.Rates.Clients;
using CarrierRatesQueryV2.Data;
using CarrierRatesQueryV2.Data.Entities;
using CarrierRatesQueryV2.Data.Seeder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace CarrierRatesQueryV2.Tests.Infrastructure;

public class TestWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _databaseName = $"TestDb_{Guid.NewGuid()}";

    public List<Carrier> SeededCarriers { get; private set; } = [];

    public void RefreshSeededCarriers()
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        SeededCarriers = db.Carriers.Include(c => c.Endpoints).ToList();
    }
    
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<AppDbContext>>();

            services.AddDbContext<AppDbContext>(options =>
                options.UseInMemoryDatabase(_databaseName));

            services.RemoveAll<IMockFedExRatesClient>();
            services.RemoveAll<IMockDhlRatesClient>();
            services.RemoveAll<IMockUpsRatesClient>();

            services.AddScoped<IMockFedExRatesClient, TestFedExRatesClient>();
            services.AddScoped<IMockDhlRatesClient, TestDhlRatesClient>();
            services.AddScoped<IMockUpsRatesClient, TestUpsRatesClient>();
        });

        builder.ConfigureServices(services =>
        {
            using var scope = services.BuildServiceProvider().CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            
            var seeder = new DataSeeder(db);
            seeder.Seed();
            
            SeededCarriers = db.Carriers.Include(c => c.Endpoints).ToList();
        });
    }

    protected override void Dispose(bool disposing)
    {
        // FastEndpoints keeps test service resolution globally; disposing a factory
        // can invalidate later endpoint tests in the same process.
    }
}
