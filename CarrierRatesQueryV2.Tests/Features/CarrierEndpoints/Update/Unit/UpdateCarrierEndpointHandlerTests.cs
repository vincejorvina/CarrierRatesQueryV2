using CarrierRatesQueryV2.Api.Features.CarrierEndpoints.Update;
using CarrierRatesQueryV2.Data;
using CarrierRatesQueryV2.Data.Entities;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using Xunit;

namespace CarrierRatesQueryV2.Tests.Features.CarrierEndpoints.Update.Unit;

public class UpdateCarrierEndpointHandlerTests
{
    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"UpdateEndpoint_{Guid.NewGuid()}")
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
        var request = new Request(Guid.NewGuid(), Guid.NewGuid(), "Rates", "https://api.test.com/rates");

        await endpoint.HandleAsync(request, CancellationToken.None);

        // Without full HTTP pipeline, Response defaults to empty response
        endpoint.Response.ShouldNotBeNull();
        endpoint.Response.Id.ShouldBe(Guid.Empty);
    }

    [Fact]
    public async Task HandleAsync_EndpointNotFound_ShouldReturnEmpty()
    {
        var db = CreateDbContext();
        var carrierId = Guid.NewGuid();
        var endpointId = Guid.NewGuid();
        db.Carriers.Add(new Carrier { Id = carrierId, Name = "Test Carrier", IsEnabled = true, CreatedAtUtc = DateTime.UtcNow });
        await db.SaveChangesAsync();

        var endpoint = CreateEndpoint(db);
        var request = new Request(carrierId, endpointId, "Rates", "https://api.test.com/rates");

        await endpoint.HandleAsync(request, CancellationToken.None);

        endpoint.Response.ShouldNotBeNull();
        endpoint.Response.Id.ShouldBe(Guid.Empty);
    }

    [Fact]
    public async Task HandleAsync_ValidRequest_ShouldUpdateEndpoint()
    {
        var db = CreateDbContext();
        var carrierId = Guid.NewGuid();
        var endpointId = Guid.NewGuid();
        db.Carriers.Add(new Carrier { Id = carrierId, Name = "Test Carrier", IsEnabled = true, CreatedAtUtc = DateTime.UtcNow });
        db.CarrierEndpoints.Add(new CarrierEndpoint { Id = endpointId, CarrierId = carrierId, Operation = "Rates", Endpoint = "https://old.test.com/rates" });
        await db.SaveChangesAsync();

        // Reload from fresh context to avoid tracking issues
        var freshDb = CreateDbContext();
        freshDb.Carriers.Add(new Carrier { Id = carrierId, Name = "Test Carrier", IsEnabled = true, CreatedAtUtc = DateTime.UtcNow });
        freshDb.CarrierEndpoints.Add(new CarrierEndpoint { Id = endpointId, CarrierId = carrierId, Operation = "Rates", Endpoint = "https://old.test.com/rates" });
        await freshDb.SaveChangesAsync();

        var endpoint = CreateEndpoint(freshDb);
        var request = new Request(carrierId, endpointId, "Rates", "https://new.test.com/rates");

        await endpoint.HandleAsync(request, CancellationToken.None);

        endpoint.Response.ShouldNotBeNull();
        endpoint.Response.Endpoint.ShouldBe("https://new.test.com/rates");
    }
}