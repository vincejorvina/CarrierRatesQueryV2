using CarrierRatesQueryV2.Api.Features.DisableRequests.Create;
using CarrierRatesQueryV2.Api.Infrastructure;
using CarrierRatesQueryV2.Data;
using CarrierRatesQueryV2.Data.Entities;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Shouldly;
using Xunit;

namespace CarrierRatesQueryV2.Tests.Features.DisableRequests.Create.Unit;

public class CreateDisableRequestHandlerTests
{
    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"CreateDisableRequest_{Guid.NewGuid()}")
            .Options;
        return new AppDbContext(options);
    }

    private static Endpoint CreateEndpoint(AppDbContext db, IRequestRoleAccessor roleAccessor)
    {
        return Factory.Create<Endpoint>(db, roleAccessor);
    }

    [Fact]
    public async Task HandleAsync_CarrierNotFound_ShouldReturnNotFound()
    {
        var db = CreateDbContext();
        var roleAccessor = Substitute.For<IRequestRoleAccessor>();
        roleAccessor.GetRequiredRole().Returns(RequestRole.Admin);
        roleAccessor.GetRequestedBy().Returns("admin");

        var endpoint = CreateEndpoint(db, roleAccessor);
        var request = new Request(Guid.NewGuid(), "Contract termination");

        await endpoint.HandleAsync(request, CancellationToken.None);

        endpoint.Response.ShouldNotBeNull();
        endpoint.Response.Id.ShouldBe(Guid.Empty);
    }

    [Fact]
    public async Task HandleAsync_ValidRequest_ShouldCreatePendingRequest()
    {
        var db = CreateDbContext();
        var carrierId = Guid.NewGuid();
        db.Carriers.Add(new Carrier { Id = carrierId, Name = "Test Carrier", IsEnabled = true, CreatedAtUtc = DateTime.UtcNow });
        await db.SaveChangesAsync();

        var roleAccessor = Substitute.For<IRequestRoleAccessor>();
        roleAccessor.GetRequiredRole().Returns(RequestRole.Admin);
        roleAccessor.GetRequestedBy().Returns("admin");

        var endpoint = CreateEndpoint(db, roleAccessor);
        var request = new Request(carrierId, "Contract termination");

        await endpoint.HandleAsync(request, CancellationToken.None);

        endpoint.Response.ShouldNotBeNull();
        endpoint.Response.CarrierId.ShouldBe(carrierId);
        endpoint.Response.Reason.ShouldBe("Contract termination");
        endpoint.Response.Status.ShouldBe("Pending");

        var savedRequest = await db.DisableRequests.FirstOrDefaultAsync(dr => dr.CarrierId == carrierId);
        savedRequest.ShouldNotBeNull();
        savedRequest.Status.ShouldBe(DisableRequestStatus.Pending);
    }
}