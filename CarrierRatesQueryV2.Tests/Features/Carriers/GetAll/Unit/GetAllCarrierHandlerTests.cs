using CarrierRatesQueryV2.Api.Features.Carriers.GetAll;
using CarrierRatesQueryV2.Data;
using CarrierRatesQueryV2.Data.Entities;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using Xunit;

namespace CarrierRatesQueryV2.Tests.Features.Carriers.GetAll.Unit;

public class GetAllCarrierHandlerTests
{
    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"GetAllCarrier_{Guid.NewGuid()}")
            .Options;
        return new AppDbContext(options);
    }

    private static Endpoint CreateEndpoint(AppDbContext db)
    {
        return Factory.Create<Endpoint>(db);
    }

    [Fact]
    public async Task HandleAsync_NoCarriers_ShouldReturnEmptyList()
    {
        var db = CreateDbContext();
        var endpoint = CreateEndpoint(db);

        await endpoint.HandleAsync(CancellationToken.None);

        endpoint.Response.ShouldNotBeNull();
        endpoint.Response.ShouldBeEmpty();
    }

    [Fact]
    public async Task HandleAsync_WithCarriers_ShouldReturnAllCarriers()
    {
        var db = CreateDbContext();
        db.Carriers.AddRange(
            new Carrier { Id = Guid.NewGuid(), Name = "Carrier B", IsEnabled = true, CreatedAtUtc = DateTime.UtcNow },
            new Carrier { Id = Guid.NewGuid(), Name = "Carrier A", IsEnabled = false, CreatedAtUtc = DateTime.UtcNow }
        );
        await db.SaveChangesAsync();

        var endpoint = CreateEndpoint(db);

        await endpoint.HandleAsync(CancellationToken.None);

        endpoint.Response.ShouldNotBeNull();
        endpoint.Response.Count.ShouldBe(2);
        endpoint.Response.First().Name.ShouldBe("Carrier A");
        endpoint.Response.Last().Name.ShouldBe("Carrier B");
    }

    [Fact]
    public async Task HandleAsync_WithCarriers_ShouldOrderByName()
    {
        var db = CreateDbContext();
        db.Carriers.AddRange(
            new Carrier { Id = Guid.NewGuid(), Name = "Zebra", IsEnabled = true, CreatedAtUtc = DateTime.UtcNow },
            new Carrier { Id = Guid.NewGuid(), Name = "Alpha", IsEnabled = true, CreatedAtUtc = DateTime.UtcNow },
            new Carrier { Id = Guid.NewGuid(), Name = "Mike", IsEnabled = true, CreatedAtUtc = DateTime.UtcNow }
        );
        await db.SaveChangesAsync();

        var endpoint = CreateEndpoint(db);

        await endpoint.HandleAsync(CancellationToken.None);

        endpoint.Response.ShouldNotBeNull();
        endpoint.Response.Count.ShouldBe(3);
        endpoint.Response[0].Name.ShouldBe("Alpha");
        endpoint.Response[1].Name.ShouldBe("Mike");
        endpoint.Response[2].Name.ShouldBe("Zebra");
    }
}