using CarrierRatesQueryV2.Api.Features.CarrierEndpoints.Delete;
using CarrierRatesQueryV2.Data;
using CarrierRatesQueryV2.Data.Entities;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using Xunit;

namespace CarrierRatesQueryV2.Tests.Features.CarrierEndpoints.Delete.Unit;

public class DeleteCarrierEndpointHandlerTests
{
    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"DeleteEndpoint_{Guid.NewGuid()}")
            .Options;
        return new AppDbContext(options);
    }

    private static Endpoint CreateEndpoint(AppDbContext db)
    {
        return Factory.Create<Endpoint>(db);
    }

    [Fact]
    public async Task HandleAsync_CarrierNotFound_ShouldNotThrow()
    {
        var db = CreateDbContext();
        var endpoint = CreateEndpoint(db);
        var request = new Request(Guid.NewGuid(), Guid.NewGuid());

        await endpoint.HandleAsync(request, CancellationToken.None);

        // Delete endpoint has no response - it just sends NoContent
    }

    [Fact]
    public async Task HandleAsync_EndpointNotFound_ShouldNotThrow()
    {
        var db = CreateDbContext();
        var carrierId = Guid.NewGuid();
        db.Carriers.Add(new Carrier { Id = carrierId, Name = "Test Carrier", IsEnabled = true, CreatedAtUtc = DateTime.UtcNow });
        await db.SaveChangesAsync();

        var endpoint = CreateEndpoint(db);
        var request = new Request(carrierId, Guid.NewGuid());

        await endpoint.HandleAsync(request, CancellationToken.None);

        // Delete endpoint has no response - it just sends NoContent
    }

    [Fact(Skip = "In-memory DB ExecuteDeleteAsync not supported - requires integration test")]
    public async Task HandleAsync_ValidRequest_ShouldDeleteEndpoint()
    {
        var db = CreateDbContext();
        var carrierId = Guid.NewGuid();
        var endpointId = Guid.NewGuid();
        db.Carriers.Add(new Carrier { Id = carrierId, Name = "Test Carrier", IsEnabled = true, CreatedAtUtc = DateTime.UtcNow });
        db.CarrierEndpoints.Add(new CarrierEndpoint { Id = endpointId, CarrierId = carrierId, Operation = "Rates", Endpoint = "https://api.test.com/rates" });
        await db.SaveChangesAsync();

        var endpoint = CreateEndpoint(db);
        var request = new Request(carrierId, endpointId);

        await endpoint.HandleAsync(request, CancellationToken.None);

        // Verify the endpoint was removed from the database
        var deletedEndpoint = await db.CarrierEndpoints.FirstOrDefaultAsync(e => e.Id == endpointId);
        deletedEndpoint.ShouldBeNull();
    }
}