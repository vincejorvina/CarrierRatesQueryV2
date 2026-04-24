using CarrierRatesQueryV2.Api.Features.CarrierEndpoints.Create;
using CarrierRatesQueryV2.Data;
using CarrierRatesQueryV2.Data.Entities;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using Xunit;

namespace CarrierRatesQueryV2.Tests.Features.CarrierEndpoints.Create.Unit;

public class CreateCarrierEndpointHandlerTests
{
    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"CreateEndpoint_{Guid.NewGuid()}")
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
        var request = new Request(Guid.NewGuid(), "Rates", "https://api.test.com/rates");

        await endpoint.HandleAsync(request, CancellationToken.None);

        // Without full HTTP pipeline, Response defaults to empty response
        endpoint.Response.ShouldNotBeNull();
        endpoint.Response.Id.ShouldBe(Guid.Empty);
    }

    [Fact]
    public async Task HandleAsync_ValidRequest_ShouldCreateEndpoint()
    {
        var db = CreateDbContext();
        var carrierId = Guid.NewGuid();
        db.Carriers.Add(new Carrier { Id = carrierId, Name = "Test Carrier", IsEnabled = true, CreatedAtUtc = DateTime.UtcNow });
        await db.SaveChangesAsync();

        var endpoint = CreateEndpoint(db);
        var request = new Request(carrierId, "Rates", "https://api.test.com/rates");

        await endpoint.HandleAsync(request, CancellationToken.None);

        endpoint.Response.ShouldNotBeNull();
        endpoint.Response.CarrierId.ShouldBe(carrierId);
        endpoint.Response.Operation.ShouldBe("Rates");
        endpoint.Response.Endpoint.ShouldBe("https://api.test.com/rates");

        var savedEndpoint = await db.CarrierEndpoints.FirstOrDefaultAsync(e => e.CarrierId == carrierId);
        savedEndpoint.ShouldNotBeNull();
        savedEndpoint.Operation.ShouldBe("Rates");
    }

    [Fact]
    public async Task HandleAsync_ValidRequest_ShouldSaveToDatabase()
    {
        var db = CreateDbContext();
        var carrierId = Guid.NewGuid();
        db.Carriers.Add(new Carrier { Id = carrierId, Name = "Test Carrier", IsEnabled = true, CreatedAtUtc = DateTime.UtcNow });
        await db.SaveChangesAsync();

        var endpoint = CreateEndpoint(db);
        var request = new Request(carrierId, "Tracking", "https://api.test.com/tracking");

        await endpoint.HandleAsync(request, CancellationToken.None);

        var allEndpoints = await db.CarrierEndpoints.ToListAsync();
        allEndpoints.Count.ShouldBe(1);
        allEndpoints[0].Operation.ShouldBe("Tracking");
    }
}