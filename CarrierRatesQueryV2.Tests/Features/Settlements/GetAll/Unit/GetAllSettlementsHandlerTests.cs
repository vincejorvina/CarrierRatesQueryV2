using CarrierRatesQueryV2.Api.Features.Settlements.GetAll;
using CarrierRatesQueryV2.Data;
using CarrierRatesQueryV2.Data.Entities;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using Xunit;

namespace CarrierRatesQueryV2.Tests.Features.Settlements.GetAll.Unit;

public class GetAllSettlementsHandlerTests
{
    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"GetAllSettlements_{Guid.NewGuid()}")
            .Options;
        return new AppDbContext(options);
    }

    private static Endpoint CreateEndpoint(AppDbContext db)
    {
        return Factory.Create<Endpoint>(db);
    }

    [Fact]
    public async Task HandleAsync_NoSettlements_ReturnsEmptyList()
    {
        var db = CreateDbContext();
        var endpoint = CreateEndpoint(db);

        await endpoint.HandleAsync(CancellationToken.None);

        endpoint.Response.ShouldNotBeNull();
        endpoint.Response.ShouldBeEmpty();
    }

    [Fact]
    public async Task HandleAsync_WithSettlements_ReturnsSettlements()
    {
        var db = CreateDbContext();
        var carrierId = Guid.NewGuid();
        db.Carriers.Add(new Carrier { Id = carrierId, Name = "FedEx", IsEnabled = true, CreatedAtUtc = DateTime.UtcNow });
        db.CarrierFinancialSettlements.Add(new CarrierFinancialSettlement
        {
            Id = Guid.NewGuid(),
            CarrierId = carrierId,
            Status = CarrierFinancialSettlementStatus.Pending,
            CreatedAtUtc = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var endpoint = CreateEndpoint(db);

        await endpoint.HandleAsync(CancellationToken.None);

        endpoint.Response.ShouldNotBeNull();
        endpoint.Response.Count.ShouldBe(1);
    }
}