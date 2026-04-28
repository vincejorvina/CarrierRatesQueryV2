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
    public List<Carrier> SeededCarriers { get; private set; } = [];

    public void RefreshSeededCarriers()
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        SeededCarriers = db.Carriers.Include(c => c.Endpoints).ToList();
    }

    public async Task ResetDatabaseAsync()
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        
        await db.Database.EnsureDeletedAsync();
        await db.Database.EnsureCreatedAsync();
        new DataSeeder(db).Seed();
        RefreshSeededCarriers();
    }
    
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<AppDbContext>>();
            services.AddDbContext<AppDbContext>(options =>
                options.UseInMemoryDatabase("TestDb"));

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
            new DataSeeder(db).Seed();
            SeededCarriers = db.Carriers.Include(c => c.Endpoints).ToList();
        });
    }

    protected override void Dispose(bool disposing) { }
}
