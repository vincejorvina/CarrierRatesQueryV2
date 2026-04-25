using CarrierRatesQueryV2.Api.Features.Shipments.Update;
using CarrierRatesQueryV2.Data;
using CarrierRatesQueryV2.Data.Entities;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using Xunit;

namespace CarrierRatesQueryV2.Tests.Features.Shipments.Update.Unit;

public class UpdateShipmentHandlerTests
{
    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"UpdateShipment_{Guid.NewGuid()}")
            .Options;
        return new AppDbContext(options);
    }

    private static Endpoint CreateEndpoint(AppDbContext db)
    {
        return Factory.Create<Endpoint>(db);
    }

    [Fact]
    public async Task HandleAsync_ValidStatus_UpdatesShipment()
    {
        var db = CreateDbContext();
        var carrierId = Guid.NewGuid();
        var shipmentId = Guid.NewGuid();
        db.Carriers.Add(new Carrier { Id = carrierId, Name = "FedEx", IsEnabled = true, CreatedAtUtc = DateTime.UtcNow });
        db.Shipments.Add(new Shipment
        {
            Id = shipmentId,
            CarrierId = carrierId,
            Status = ShipmentStatus.Pending,
            CreatedAtUtc = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var endpoint = CreateEndpoint(db);
        var request = new Request(shipmentId, "Completed");

        await endpoint.HandleAsync(request, CancellationToken.None);

        endpoint.Response.ShouldNotBeNull();
        endpoint.Response.Status.ShouldBe("Completed");
    }

    [Fact]
    public async Task HandleAsync_PendingStatus_ReturnsPending()
    {
        var db = CreateDbContext();
        var carrierId = Guid.NewGuid();
        var shipmentId = Guid.NewGuid();
        db.Shipments.Add(new Shipment
        {
            Id = shipmentId,
            CarrierId = carrierId,
            Status = ShipmentStatus.Pending,
            CreatedAtUtc = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var endpoint = CreateEndpoint(db);
        var request = new Request(shipmentId, "Pending");

        await endpoint.HandleAsync(request, CancellationToken.None);

        endpoint.Response.ShouldNotBeNull();
        endpoint.Response.Status.ShouldBe("Pending");
    }
}