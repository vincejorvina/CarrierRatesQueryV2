using System;
using CarrierRatesQueryV2.Api.Features.DisableRequests.Approve;
using CarrierRatesQueryV2.Api.Infrastructure;
using CarrierRatesQueryV2.Api.Services;
using CarrierRatesQueryV2.Data;
using CarrierRatesQueryV2.Data.Entities;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Shouldly;
using Xunit;

namespace CarrierRatesQueryV2.Tests.Features.DisableRequests.Approve.Unit;

public class ApproveDisableRequestHandlerTests
{
    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"ApproveDisableRequest_{Guid.NewGuid()}")
            .Options;
        return new AppDbContext(options);
    }

    private static Endpoint CreateEndpoint(AppDbContext db, IRequestRoleAccessor roleAccessor, ICarrierManagementService? carrierService = null)
    {
        return Factory.Create<Endpoint>(db, roleAccessor, carrierService ?? Substitute.For<ICarrierManagementService>());
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
    public async Task HandleAsync_RequestNotFound_ShouldReturnNotFound()
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
            Status = DisableRequestStatus.Approved,
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
    public async Task HandleAsync_ValidPendingRequest_ShouldApprove()
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

        var carrierService = Substitute.For<ICarrierManagementService>();
        carrierService.ValidateCanDisableCarrierAsync(Arg.Any<Guid>(), Arg.Any<AppDbContext>()).Returns((true, null));

        var endpoint = CreateEndpoint(db, roleAccessor, carrierService);
        var request = new Request(requestId);

        await endpoint.HandleAsync(request, CancellationToken.None);

        endpoint.Response.ShouldNotBeNull();
        endpoint.Response.Status.ShouldBe("Approved");
    }

    [Fact]
    public async Task HandleAsync_PendingRequestsForOtherCarriers_ShouldFail()
    {
        var db = CreateDbContext();
        var carrierAId = Guid.NewGuid();
        var carrierBId = Guid.NewGuid();
        var carrierCId = Guid.NewGuid();
        var requestAId = Guid.NewGuid();

        db.Carriers.Add(new Carrier { Id = carrierAId, Name = "Carrier A", IsEnabled = true, CreatedAtUtc = DateTime.UtcNow });
        db.Carriers.Add(new Carrier { Id = carrierBId, Name = "Carrier B", IsEnabled = true, CreatedAtUtc = DateTime.UtcNow });
        db.Carriers.Add(new Carrier { Id = carrierCId, Name = "Carrier C", IsEnabled = true, CreatedAtUtc = DateTime.UtcNow });

        db.DisableRequests.Add(new DisableRequest
        {
            Id = requestAId,
            CarrierId = carrierAId,
            RequestedBy = "user",
            Reason = "Disable A",
            Status = DisableRequestStatus.Pending,
            RequestedAtUtc = DateTime.UtcNow
        });

        db.DisableRequests.Add(new DisableRequest
        {
            Id = Guid.NewGuid(),
            CarrierId = carrierBId,
            RequestedBy = "user2",
            Reason = "Disable B",
            Status = DisableRequestStatus.Pending,
            RequestedAtUtc = DateTime.UtcNow
        });
        db.DisableRequests.Add(new DisableRequest
        {
            Id = Guid.NewGuid(),
            CarrierId = carrierCId,
            RequestedBy = "user3",
            Reason = "Disable C",
            Status = DisableRequestStatus.Pending,
            RequestedAtUtc = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var roleAccessor = Substitute.For<IRequestRoleAccessor>();
        roleAccessor.GetRequiredRole().Returns(RequestRole.Admin);
        roleAccessor.GetRequestedBy().Returns("admin");

        var carrierService = Substitute.For<ICarrierManagementService>();
        carrierService.ValidateCanDisableCarrierAsync(Arg.Any<Guid>(), Arg.Any<AppDbContext>()).Returns((false, "Cannot disable carrier: would leave no enabled carriers after accounting for all pending disable requests."));

        var endpoint = CreateEndpoint(db, roleAccessor, carrierService);
        var request = new Request(requestAId);

        await Assert.ThrowsAsync<FastEndpoints.ValidationFailureException>(async () =>
            await endpoint.HandleAsync(request, CancellationToken.None));
    }
}