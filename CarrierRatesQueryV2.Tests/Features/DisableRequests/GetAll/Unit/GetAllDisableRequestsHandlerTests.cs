using CarrierRatesQueryV2.Api.Features.DisableRequests.GetAll;
using CarrierRatesQueryV2.Api.Infrastructure;
using CarrierRatesQueryV2.Data;
using CarrierRatesQueryV2.Data.Entities;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Shouldly;
using Xunit;

namespace CarrierRatesQueryV2.Tests.Features.DisableRequests.GetAll.Unit;

public class GetAllDisableRequestsHandlerTests
{
    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"GetAllDisableRequests_{Guid.NewGuid()}")
            .Options;
        return new AppDbContext(options);
    }

    private static Endpoint CreateEndpoint(AppDbContext db, IRequestRoleAccessor roleAccessor)
    {
        return Factory.Create<Endpoint>(db, roleAccessor);
    }

    [Fact]
    public async Task HandleAsync_NoRequests_ShouldReturnEmptyList()
    {
        var db = CreateDbContext();
        var roleAccessor = Substitute.For<IRequestRoleAccessor>();
        roleAccessor.GetRequiredRole().Returns(RequestRole.Admin);

        var endpoint = CreateEndpoint(db, roleAccessor);

        await endpoint.HandleAsync(CancellationToken.None);

        endpoint.Response.ShouldNotBeNull();
        endpoint.Response.ShouldBeEmpty();
    }

    [Fact]
    public async Task HandleAsync_WithRequests_ShouldReturnAllRequests()
    {
        var db = CreateDbContext();
        var carrierId = Guid.NewGuid();
        db.DisableRequests.AddRange(
            new DisableRequest { Id = Guid.NewGuid(), CarrierId = carrierId, RequestedBy = "user1", Reason = "Reason 1", Status = DisableRequestStatus.Pending, RequestedAtUtc = DateTime.UtcNow.AddDays(-1) },
            new DisableRequest { Id = Guid.NewGuid(), CarrierId = carrierId, RequestedBy = "user2", Reason = "Reason 2", Status = DisableRequestStatus.Approved, RequestedAtUtc = DateTime.UtcNow }
        );
        await db.SaveChangesAsync();

        var roleAccessor = Substitute.For<IRequestRoleAccessor>();
        roleAccessor.GetRequiredRole().Returns(RequestRole.Admin);

        var endpoint = CreateEndpoint(db, roleAccessor);

        await endpoint.HandleAsync(CancellationToken.None);

        endpoint.Response.ShouldNotBeNull();
        endpoint.Response.Count.ShouldBe(2);
    }

    [Fact]
    public async Task HandleAsync_WithRequests_ShouldOrderByDateDescending()
    {
        var db = CreateDbContext();
        var carrierId = Guid.NewGuid();
        db.DisableRequests.AddRange(
            new DisableRequest { Id = Guid.NewGuid(), CarrierId = carrierId, RequestedBy = "user1", Reason = "Old", Status = DisableRequestStatus.Pending, RequestedAtUtc = DateTime.UtcNow.AddDays(-1) },
            new DisableRequest { Id = Guid.NewGuid(), CarrierId = carrierId, RequestedBy = "user2", Reason = "New", Status = DisableRequestStatus.Pending, RequestedAtUtc = DateTime.UtcNow }
        );
        await db.SaveChangesAsync();

        var roleAccessor = Substitute.For<IRequestRoleAccessor>();
        roleAccessor.GetRequiredRole().Returns(RequestRole.Admin);

        var endpoint = CreateEndpoint(db, roleAccessor);

        await endpoint.HandleAsync(CancellationToken.None);

        endpoint.Response.ShouldNotBeNull();
        endpoint.Response.First().Reason.ShouldBe("New");
    }
}