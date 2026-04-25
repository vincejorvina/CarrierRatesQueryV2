using CarrierRatesQueryV2.Api.Features.Shipments.Delete;
using CarrierRatesQueryV2.Data;
using CarrierRatesQueryV2.Data.Entities;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using Xunit;

namespace CarrierRatesQueryV2.Tests.Features.Shipments.Delete.Unit;

public class DeleteShipmentHandlerTests
{
    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"DeleteShipment_{Guid.NewGuid()}")
            .Options;
        return new AppDbContext(options);
    }

    private static Endpoint CreateEndpoint(AppDbContext db)
    {
        return Factory.Create<Endpoint>(db);
    }

    [Fact]
    public async Task HandleAsync_ExistingShipment_ReturnsNoContent()
    {
        var db = CreateDbContext();
        var shipmentId = Guid.NewGuid();
        db.Shipments.Add(new Shipment
        {
            Id = shipmentId,
            CarrierId = Guid.NewGuid(),
            Status = ShipmentStatus.Pending,
            CreatedAtUtc = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var endpoint = CreateEndpoint(db);
        var request = new Request(shipmentId);

        await endpoint.HandleAsync(request, CancellationToken.None);

        endpoint.HttpContext.Response.StatusCode.ShouldBe(204);
    }

    [Fact]
    public async Task HandleAsync_NonExistentShipment_Returns404()
    {
        var db = CreateDbContext();

        var endpoint = CreateEndpoint(db);
        var request = new Request(Guid.NewGuid());

        await endpoint.HandleAsync(request, CancellationToken.None);

        endpoint.HttpContext.Response.StatusCode.ShouldBe(404);
    }
}