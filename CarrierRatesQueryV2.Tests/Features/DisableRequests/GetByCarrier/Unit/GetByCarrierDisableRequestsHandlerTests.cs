using CarrierRatesQueryV2.Api.Features.DisableRequests.GetByCarrier;
using CarrierRatesQueryV2.Api.Infrastructure;
using CarrierRatesQueryV2.Data;
using CarrierRatesQueryV2.Data.Entities;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Shouldly;
using Xunit;

namespace CarrierRatesQueryV2.Tests.Features.DisableRequests.GetByCarrier.Unit;

public class GetByCarrierDisableRequestsHandlerTests
{
    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"GetByCarrierDisableRequests_{Guid.NewGuid()}")
            .Options;
        return new AppDbContext(options);
    }

    private static Endpoint CreateEndpoint(AppDbContext db, IRequestRoleAccessor roleAccessor)
    {
        return Factory.Create<Endpoint>(db, roleAccessor);
    }

    [Fact]
    public async Task HandleAsync_CarrierNotFound_ShouldReturnEmpty()
    {
        var db = CreateDbContext();
        var roleAccessor = Substitute.For<IRequestRoleAccessor>();
        roleAccessor.GetRequiredRole().Returns(RequestRole.Admin);

        var endpoint = CreateEndpoint(db, roleAccessor);
        var request = new Request(Guid.NewGuid());

        await endpoint.HandleAsync(request, CancellationToken.None);

        endpoint.Response.ShouldNotBeNull();
    }

    [Fact]
    public async Task HandleAsync_NoRequestsForCarrier_ShouldReturnEmptyList()
    {
        var db = CreateDbContext();
        var carrierId = Guid.NewGuid();
        db.Carriers.Add(new Carrier { Id = carrierId, Name = "Test Carrier", IsEnabled = true, CreatedAtUtc = DateTime.UtcNow });
        await db.SaveChangesAsync();

        var roleAccessor = Substitute.For<IRequestRoleAccessor>();
        roleAccessor.GetRequiredRole().Returns(RequestRole.Admin);

        var endpoint = CreateEndpoint(db, roleAccessor);
        var request = new Request(carrierId);

        await endpoint.HandleAsync(request, CancellationToken.None);

        endpoint.Response.ShouldNotBeNull();
        endpoint.Response.ShouldBeEmpty();
    }

    [Fact]
    public async Task HandleAsync_WithRequests_ShouldReturnRequestsForCarrier()
    {
        var db = CreateDbContext();
        var carrierId = Guid.NewGuid();
        db.Carriers.Add(new Carrier { Id = carrierId, Name = "Test Carrier", IsEnabled = true, CreatedAtUtc = DateTime.UtcNow });
        db.DisableRequests.AddRange(
            new DisableRequest { Id = Guid.NewGuid(), CarrierId = carrierId, RequestedBy = "user1", Reason = "Reason 1", Status = DisableRequestStatus.Pending, RequestedAtUtc = DateTime.UtcNow },
            new DisableRequest { Id = Guid.NewGuid(), CarrierId = Guid.NewGuid(), RequestedBy = "user2", Reason = "Other", Status = DisableRequestStatus.Pending, RequestedAtUtc = DateTime.UtcNow }
        );
        await db.SaveChangesAsync();

        var roleAccessor = Substitute.For<IRequestRoleAccessor>();
        roleAccessor.GetRequiredRole().Returns(RequestRole.Admin);

        var endpoint = CreateEndpoint(db, roleAccessor);
        var request = new Request(carrierId);

        await endpoint.HandleAsync(request, CancellationToken.None);

        endpoint.Response.ShouldNotBeNull();
        endpoint.Response.Count.ShouldBe(1);
        endpoint.Response.First().CarrierId.ShouldBe(carrierId);
    }
}