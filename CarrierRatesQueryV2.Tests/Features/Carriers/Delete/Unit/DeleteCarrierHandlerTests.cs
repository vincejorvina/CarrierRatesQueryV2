using CarrierRatesQueryV2.Api.Features.Carriers.Delete;
using CarrierRatesQueryV2.Data;
using CarrierRatesQueryV2.Data.Entities;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using Xunit;

namespace CarrierRatesQueryV2.Tests.Features.Carriers.Delete.Unit;

public class DeleteCarrierHandlerTests
{
    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"DeleteCarrier_{Guid.NewGuid()}")
            .Options;
        return new AppDbContext(options);
    }

    private static Endpoint CreateEndpoint(AppDbContext db)
    {
        return Factory.Create<Endpoint>(db);
    }

    [Fact]
    public async Task HandleAsync_ValidId_ShouldDeleteCarrier()
    {
        var db = CreateDbContext();
        var carrierId = Guid.NewGuid();
        db.Carriers.Add(new Carrier
        {
            Id = carrierId,
            Name = "Test Carrier",
            IsEnabled = true,
            CreatedAtUtc = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var endpoint = CreateEndpoint(db);
        var request = new Request(carrierId);

        await endpoint.HandleAsync(request, CancellationToken.None);

        var deletedCarrier = await db.Carriers.FirstOrDefaultAsync(c => c.Id == carrierId);
        deletedCarrier.ShouldBeNull();
    }

    [Fact]
    public async Task HandleAsync_NonExistentId_ShouldReturnNotFound()
    {
        var db = CreateDbContext();
        var endpoint = CreateEndpoint(db);
        var request = new Request(Guid.NewGuid());

        await endpoint.HandleAsync(request, CancellationToken.None);

        endpoint.Response.ShouldBeNull();
    }

    [Fact]
    public async Task HandleAsync_WithEndpoints_ShouldCascadeDeleteEndpoints()
    {
        var db = CreateDbContext();
        var carrierId = Guid.NewGuid();
        db.Carriers.Add(new Carrier
        {
            Id = carrierId,
            Name = "Test Carrier",
            IsEnabled = true,
            CreatedAtUtc = DateTime.UtcNow,
            Endpoints = new List<CarrierEndpoint>
            {
                new() { Id = Guid.NewGuid(), CarrierId = carrierId, Operation = "Rates", Endpoint = "https://api.test.com/rates" }
            }
        });
        await db.SaveChangesAsync();

        var endpoint = CreateEndpoint(db);
        var request = new Request(carrierId);

        await endpoint.HandleAsync(request, CancellationToken.None);

        var deletedCarrier = await db.Carriers.FirstOrDefaultAsync(c => c.Id == carrierId);
        deletedCarrier.ShouldBeNull();

        var endpoints = await db.CarrierEndpoints.Where(e => e.CarrierId == carrierId).ToListAsync();
        endpoints.ShouldBeEmpty();
    }
}