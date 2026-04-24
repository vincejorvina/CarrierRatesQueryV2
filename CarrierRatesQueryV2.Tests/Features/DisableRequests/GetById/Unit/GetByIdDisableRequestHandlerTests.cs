using CarrierRatesQueryV2.Api.Features.DisableRequests.GetById;
using CarrierRatesQueryV2.Data;
using CarrierRatesQueryV2.Data.Entities;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using Xunit;

namespace CarrierRatesQueryV2.Tests.Features.DisableRequests.GetById.Unit;

public class GetByIdDisableRequestHandlerTests
{
    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"GetByIdDisableRequest_{Guid.NewGuid()}")
            .Options;
        return new AppDbContext(options);
    }

    private static Endpoint CreateEndpoint(AppDbContext db)
    {
        return Factory.Create<Endpoint>(db);
    }

    [Fact]
    public async Task HandleAsync_ValidId_ShouldReturnRequest()
    {
        var db = CreateDbContext();
        var requestId = Guid.NewGuid();
        var carrierId = Guid.NewGuid();
        db.DisableRequests.Add(new DisableRequest
        {
            Id = requestId,
            CarrierId = carrierId,
            RequestedBy = "testuser",
            Reason = "Contract termination",
            Status = DisableRequestStatus.Pending,
            RequestedAtUtc = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var endpoint = CreateEndpoint(db);
        var request = new Request(requestId);

        await endpoint.HandleAsync(request, CancellationToken.None);

        endpoint.Response.ShouldNotBeNull();
        endpoint.Response.Id.ShouldBe(requestId);
        endpoint.Response.Reason.ShouldBe("Contract termination");
    }

    [Fact]
    public async Task HandleAsync_InvalidId_ShouldReturnEmpty()
    {
        var db = CreateDbContext();

        var endpoint = CreateEndpoint(db);
        var request = new Request(Guid.NewGuid());

        await endpoint.HandleAsync(request, CancellationToken.None);

        endpoint.Response.ShouldNotBeNull();
        endpoint.Response.Id.ShouldBe(Guid.Empty);
    }
}