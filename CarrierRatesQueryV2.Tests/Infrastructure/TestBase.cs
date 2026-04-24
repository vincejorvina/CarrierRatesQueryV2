using CarrierRatesQueryV2.Data;
using CarrierRatesQueryV2.Api;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace CarrierRatesQueryV2.Tests.Infrastructure;

public abstract class TestBase<TEndpoint> : IDisposable
    where TEndpoint : BaseEndpoint
{
    protected AppDbContext Db { get; }
    protected TEndpoint Endpoint { get; }

    protected TestBase()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        Db = new AppDbContext(options);
        Endpoint = Factory.Create<TEndpoint>(Db);
    }

    public void Dispose()
    {
        Db.Dispose();
    }
}