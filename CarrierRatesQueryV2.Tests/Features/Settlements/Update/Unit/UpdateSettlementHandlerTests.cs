using CarrierRatesQueryV2.Api.Features.Settlements.Update;
using CarrierRatesQueryV2.Data;
using CarrierRatesQueryV2.Data.Entities;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using Xunit;

namespace CarrierRatesQueryV2.Tests.Features.Settlements.Update.Unit;

public class UpdateSettlementHandlerTests
{
    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"UpdateSettlement_{Guid.NewGuid()}")
            .Options;
        return new AppDbContext(options);
    }

    private static Endpoint CreateEndpoint(AppDbContext db)
    {
        return Factory.Create<Endpoint>(db);
    }

    [Fact]
    public async Task HandleAsync_ValidStatus_UpdatesSettlement()
    {
        var db = CreateDbContext();
        var carrierId = Guid.NewGuid();
        var settlementId = Guid.NewGuid();
        db.Carriers.Add(new Carrier { Id = carrierId, Name = "FedEx", IsEnabled = true, CreatedAtUtc = DateTime.UtcNow });
        db.CarrierFinancialSettlements.Add(new CarrierFinancialSettlement
        {
            Id = settlementId,
            CarrierId = carrierId,
            Status = CarrierFinancialSettlementStatus.Pending,
            CreatedAtUtc = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var endpoint = CreateEndpoint(db);
        var request = new Request(settlementId, "Settled");

        await endpoint.HandleAsync(request, CancellationToken.None);

        endpoint.Response.ShouldNotBeNull();
        endpoint.Response.Status.ShouldBe("Settled");
    }

    [Fact]
    public async Task HandleAsync_PendingStatus_ReturnsPending()
    {
        var db = CreateDbContext();
        var carrierId = Guid.NewGuid();
        var settlementId = Guid.NewGuid();
        db.CarrierFinancialSettlements.Add(new CarrierFinancialSettlement
        {
            Id = settlementId,
            CarrierId = carrierId,
            Status = CarrierFinancialSettlementStatus.Pending,
            CreatedAtUtc = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var endpoint = CreateEndpoint(db);
        var request = new Request(settlementId, "Pending");

        await endpoint.HandleAsync(request, CancellationToken.None);

        endpoint.Response.ShouldNotBeNull();
        endpoint.Response.Status.ShouldBe("Pending");
    }
}