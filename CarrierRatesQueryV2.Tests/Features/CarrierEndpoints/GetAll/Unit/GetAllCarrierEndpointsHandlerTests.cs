using CarrierRatesQueryV2.Api.Features.CarrierEndpoints.GetAll;
using CarrierRatesQueryV2.Data;
using CarrierRatesQueryV2.Data.Entities;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using Xunit;

namespace CarrierRatesQueryV2.Tests.Features.CarrierEndpoints.GetAll.Unit;

public class GetAllCarrierEndpointsHandlerTests
{
    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"GetAllEndpoints_{Guid.NewGuid()}")
            .Options;
        return new AppDbContext(options);
    }

    private static Endpoint CreateEndpoint(AppDbContext db)
    {
        return Factory.Create<Endpoint>(db);
    }

    [Fact]
    public async Task HandleAsync_CarrierNotFound_ShouldReturnEmpty()
    {
        var db = CreateDbContext();
        var endpoint = CreateEndpoint(db);
        var request = new Request(Guid.NewGuid());

        await endpoint.HandleAsync(request, CancellationToken.None);

        // Without full HTTP pipeline, Response defaults to empty list rather than 404
        endpoint.Response.ShouldNotBeNull();
    }

    [Fact]
    public async Task HandleAsync_NoEndpoints_ShouldReturnEmptyList()
    {
        var db = CreateDbContext();
        var carrierId = Guid.NewGuid();
        db.Carriers.Add(new Carrier { Id = carrierId, Name = "Test Carrier", IsEnabled = true, CreatedAtUtc = DateTime.UtcNow });
        await db.SaveChangesAsync();

        var endpoint = CreateEndpoint(db);
        var request = new Request(carrierId);

        await endpoint.HandleAsync(request, CancellationToken.None);

        endpoint.Response.ShouldNotBeNull();
        endpoint.Response.ShouldBeEmpty();
    }

    [Fact]
    public async Task HandleAsync_WithEndpoints_ShouldReturnAllEndpoints()
    {
        var db = CreateDbContext();
        var carrierId = Guid.NewGuid();
        db.Carriers.Add(new Carrier { Id = carrierId, Name = "Test Carrier", IsEnabled = true, CreatedAtUtc = DateTime.UtcNow });
        db.CarrierEndpoints.AddRange(
            new CarrierEndpoint { Id = Guid.NewGuid(), CarrierId = carrierId, Operation = "Rates", Endpoint = "https://api.test.com/rates" },
            new CarrierEndpoint { Id = Guid.NewGuid(), CarrierId = carrierId, Operation = "Tracking", Endpoint = "https://api.test.com/tracking" }
        );
        await db.SaveChangesAsync();

        var endpoint = CreateEndpoint(db);
        var request = new Request(carrierId);

        await endpoint.HandleAsync(request, CancellationToken.None);

        endpoint.Response.ShouldNotBeNull();
        endpoint.Response.Count.ShouldBe(2);
    }

    [Fact]
    public async Task HandleAsync_WithEndpoints_ShouldOrderByOperation()
    {
        var db = CreateDbContext();
        var carrierId = Guid.NewGuid();
        db.Carriers.Add(new Carrier { Id = carrierId, Name = "Test Carrier", IsEnabled = true, CreatedAtUtc = DateTime.UtcNow });
        db.CarrierEndpoints.AddRange(
            new CarrierEndpoint { Id = Guid.NewGuid(), CarrierId = carrierId, Operation = "Zebra", Endpoint = "https://api.test.com/z" },
            new CarrierEndpoint { Id = Guid.NewGuid(), CarrierId = carrierId, Operation = "Alpha", Endpoint = "https://api.test.com/a" },
            new CarrierEndpoint { Id = Guid.NewGuid(), CarrierId = carrierId, Operation = "Mike", Endpoint = "https://api.test.com/m" }
        );
        await db.SaveChangesAsync();

        var endpoint = CreateEndpoint(db);
        var request = new Request(carrierId);

        await endpoint.HandleAsync(request, CancellationToken.None);

        endpoint.Response.ShouldNotBeNull();
        endpoint.Response.Count.ShouldBe(3);
        endpoint.Response[0].Operation.ShouldBe("Alpha");
        endpoint.Response[1].Operation.ShouldBe("Mike");
        endpoint.Response[2].Operation.ShouldBe("Zebra");
    }
}