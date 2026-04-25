using CarrierRatesQueryV2.Api;
using CarrierRatesQueryV2.Data;
using CarrierRatesQueryV2.Data.Entities;
using CarrierRatesQueryV2.Data.Seeder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CarrierRatesQueryV2.Tests.Infrastructure;

public class TestWebApplicationFactory : WebApplicationFactory<Program>
{
    public List<Carrier> SeededCarriers { get; private set; } = [];
    
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
            if (descriptor != null)
                services.Remove(descriptor);

            services.AddDbContext<AppDbContext>(options =>
                options.UseInMemoryDatabase($"TestDb_{Guid.NewGuid()}"));
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
}