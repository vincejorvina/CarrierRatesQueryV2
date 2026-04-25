using CarrierRatesQueryV2.Api.Features.Settlements.Delete;
using CarrierRatesQueryV2.Data;
using CarrierRatesQueryV2.Data.Entities;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using Xunit;

namespace CarrierRatesQueryV2.Tests.Features.Settlements.Delete.Unit;

public class DeleteSettlementHandlerTests
{
    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"DeleteSettlement_{Guid.NewGuid()}")
            .Options;
        return new AppDbContext(options);
    }

    private static Endpoint CreateEndpoint(AppDbContext db)
    {
        return Factory.Create<Endpoint>(db);
    }

    [Fact]
    public async Task HandleAsync_ExistingSettlement_ReturnsNoContent()
    {
        var db = CreateDbContext();
        var settlementId = Guid.NewGuid();
        db.CarrierFinancialSettlements.Add(new CarrierFinancialSettlement
        {
            Id = settlementId,
            CarrierId = Guid.NewGuid(),
            Status = CarrierFinancialSettlementStatus.Pending,
            CreatedAtUtc = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var endpoint = CreateEndpoint(db);
        var request = new Request(settlementId);

        await endpoint.HandleAsync(request, CancellationToken.None);

        endpoint.HttpContext.Response.StatusCode.ShouldBe(204);
    }

    [Fact]
    public async Task HandleAsync_NonExistentSettlement_Returns404()
    {
        var db = CreateDbContext();

        var endpoint = CreateEndpoint(db);
        var request = new Request(Guid.NewGuid());

        await endpoint.HandleAsync(request, CancellationToken.None);

        endpoint.HttpContext.Response.StatusCode.ShouldBe(404);
    }
}