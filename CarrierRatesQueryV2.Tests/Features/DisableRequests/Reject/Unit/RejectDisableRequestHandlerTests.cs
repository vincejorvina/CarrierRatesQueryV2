using System;
using CarrierRatesQueryV2.Api.Features.DisableRequests.Reject;
using CarrierRatesQueryV2.Api.Infrastructure;
using CarrierRatesQueryV2.Data;
using CarrierRatesQueryV2.Data.Entities;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Shouldly;
using Xunit;

namespace CarrierRatesQueryV2.Tests.Features.DisableRequests.Reject.Unit;

public class RejectDisableRequestHandlerTests
{
    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"RejectDisableRequest_{Guid.NewGuid()}")
            .Options;
        return new AppDbContext(options);
    }

    private static Endpoint CreateEndpoint(AppDbContext db, IRequestRoleAccessor roleAccessor)
    {
        return Factory.Create<Endpoint>(db, roleAccessor);
    }

    [Fact]
    public async Task HandleAsync_NotAdmin_ShouldThrowError()
    {
        var db = CreateDbContext();
        var roleAccessor = Substitute.For<IRequestRoleAccessor>();
        roleAccessor.GetRequiredRole().Returns(RequestRole.User);

        var endpoint = CreateEndpoint(db, roleAccessor);
        var request = new Request(Guid.NewGuid());

        await Assert.ThrowsAsync<FastEndpoints.ValidationFailureException>(async () =>
            await endpoint.HandleAsync(request, CancellationToken.None));
    }

    [Fact]
    public async Task HandleAsync_RequestNotFound_ShouldReturnEmpty()
    {
        var db = CreateDbContext();
        var roleAccessor = Substitute.For<IRequestRoleAccessor>();
        roleAccessor.GetRequiredRole().Returns(RequestRole.Admin);
        roleAccessor.GetRequestedBy().Returns("admin");

        var endpoint = CreateEndpoint(db, roleAccessor);
        var request = new Request(Guid.NewGuid());

        await endpoint.HandleAsync(request, CancellationToken.None);

        endpoint.Response.ShouldNotBeNull();
        endpoint.Response.Id.ShouldBe(Guid.Empty);
    }

    [Fact]
    public async Task HandleAsync_NotPending_ShouldThrowError()
    {
        var db = CreateDbContext();
        var carrierId = Guid.NewGuid();
        var requestId = Guid.NewGuid();
        db.Carriers.Add(new Carrier { Id = carrierId, Name = "Test Carrier", IsEnabled = true, CreatedAtUtc = DateTime.UtcNow });
        db.DisableRequests.Add(new DisableRequest
        {
            Id = requestId,
            CarrierId = carrierId,
            RequestedBy = "user",
            Reason = "Test",
            Status = DisableRequestStatus.Rejected,
            RequestedAtUtc = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var roleAccessor = Substitute.For<IRequestRoleAccessor>();
        roleAccessor.GetRequiredRole().Returns(RequestRole.Admin);
        roleAccessor.GetRequestedBy().Returns("admin");

        var endpoint = CreateEndpoint(db, roleAccessor);
        var request = new Request(requestId);

        await Assert.ThrowsAsync<FastEndpoints.ValidationFailureException>(async () =>
            await endpoint.HandleAsync(request, CancellationToken.None));
    }

    [Fact]
    public async Task HandleAsync_ValidPendingRequest_ShouldReject()
    {
        var db = CreateDbContext();
        var carrierId = Guid.NewGuid();
        var requestId = Guid.NewGuid();
        db.Carriers.Add(new Carrier { Id = carrierId, Name = "Test Carrier", IsEnabled = true, CreatedAtUtc = DateTime.UtcNow });
        db.DisableRequests.Add(new DisableRequest
        {
            Id = requestId,
            CarrierId = carrierId,
            RequestedBy = "user",
            Reason = "Test",
            Status = DisableRequestStatus.Pending,
            RequestedAtUtc = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var roleAccessor = Substitute.For<IRequestRoleAccessor>();
        roleAccessor.GetRequiredRole().Returns(RequestRole.Admin);
        roleAccessor.GetRequestedBy().Returns("admin");

        var endpoint = CreateEndpoint(db, roleAccessor);
        var request = new Request(requestId);

        await endpoint.HandleAsync(request, CancellationToken.None);

        endpoint.Response.ShouldNotBeNull();
        endpoint.Response.Status.ShouldBe("Rejected");
    }
}