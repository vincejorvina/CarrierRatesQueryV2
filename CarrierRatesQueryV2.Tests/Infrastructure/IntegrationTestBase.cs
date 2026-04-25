using CarrierRatesQueryV2.Api;
using CarrierRatesQueryV2.Data;
using CarrierRatesQueryV2.Data.Seeder;
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
    protected AppDbContext? DbContext;
    
    protected IntegrationTestBase(TestWebApplicationFactory factory)
    {
        Factory = factory;
    }
    
    public virtual async Task InitializeAsync()
    {
        Client = Factory.CreateClient();
        
        using var scope = Factory.Services.CreateScope();
        DbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        
        await DbContext.Database.EnsureDeletedAsync();
        await DbContext.Database.EnsureCreatedAsync();
        new DataSeeder(DbContext).Seed();
    }
    
    public virtual Task DisposeAsync() => Task.CompletedTask;
}