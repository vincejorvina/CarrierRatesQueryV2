using CarrierRatesQueryV2.Api.Features.CarrierEndpoints.GetById;
using CarrierRatesQueryV2.Data;
using CarrierRatesQueryV2.Data.Entities;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using Xunit;

namespace CarrierRatesQueryV2.Tests.Features.CarrierEndpoints.GetById.Unit;

public class GetByIdCarrierEndpointHandlerTests
{
    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"GetByIdEndpoint_{Guid.NewGuid()}")
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
        var request = new Request(Guid.NewGuid(), Guid.NewGuid());

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
        var request = new Request(carrierId, endpointId);

        await endpoint.HandleAsync(request, CancellationToken.None);

        endpoint.Response.ShouldNotBeNull();
        endpoint.Response.Id.ShouldBe(Guid.Empty);
    }

    [Fact]
    public async Task HandleAsync_ValidRequest_ShouldReturnEndpoint()
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

        endpoint.Response.ShouldNotBeNull();
        endpoint.Response.Id.ShouldBe(endpointId);
        endpoint.Response.Operation.ShouldBe("Rates");
        endpoint.Response.Endpoint.ShouldBe("https://api.test.com/rates");
    }
}