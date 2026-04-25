using CarrierRatesQueryV2.Api;
using CarrierRatesQueryV2.Data;
using CarrierRatesQueryV2.Data.Seeder;
using CarrierRatesQueryV2.Data.Entities;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace CarrierRatesQueryV2.Tests.Infrastructure;

[CollectionDefinition("IntegrationTests", DisableParallelization = true)]
public class IntegrationTestBase : 
    IClassFixture<TestWebApplicationFactory>,
    IAsyncLifetime
{
    protected readonly TestWebApplicationFactory Factory;
    protected HttpClient Client = null!;
    public List<Carrier> SeededCarriers => Factory.SeededCarriers;
    
    protected IntegrationTestBase(TestWebApplicationFactory factory)
    {
        Factory = factory;
    }
    
    public virtual async Task InitializeAsync()
    {
        Client = Factory.CreateClient();
        
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        
        await db.Database.EnsureDeletedAsync();
        await db.Database.EnsureCreatedAsync();
        new DataSeeder(db).Seed();
    }
    
    public virtual Task DisposeAsync() => Task.CompletedTask;
}