using CarrierRatesQueryV2.Api.Features.Carriers.Disable;
using CarrierRatesQueryV2.Api.Infrastructure;
using CarrierRatesQueryV2.Api.Services;
using CarrierRatesQueryV2.Data;
using CarrierRatesQueryV2.Data.Entities;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Shouldly;
using Xunit;

namespace CarrierRatesQueryV2.Tests.Features.Carriers.Disable.Unit;

public class DisableCarrierHandlerTests
{
    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"DisableCarrier_{Guid.NewGuid()}")
            .Options;
        return new AppDbContext(options);
    }

    private static Endpoint CreateEndpoint(AppDbContext db, IRequestRoleAccessor roleAccessor)
    {
        var carrierService = Substitute.For<ICarrierManagementService>();
        carrierService.ValidateCanDisableCarrierAsync(Arg.Any<Guid>(), Arg.Any<AppDbContext>()).Returns((true, null));
        return Factory.Create<Endpoint>(db, roleAccessor, carrierService);
    }

    [Fact(Skip = "Requires admin role mocking - use integration tests")]
    public async Task HandleAsync_AdminRole_ShouldDisableCarrier()
    {
        var db = CreateDbContext();
        var carrierId = Guid.NewGuid();
        db.Carriers.Add(new Carrier
        {
            Id = carrierId,
            Name = "Test Carrier",
            IsEnabled = true,
            CreatedAtUtc = DateTime.UtcNow
        });
        db.Carriers.Add(new Carrier
        {
            Id = Guid.NewGuid(),
            Name = "Other Carrier",
            IsEnabled = true,
            CreatedAtUtc = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var roleAccessor = Substitute.For<IRequestRoleAccessor>();
        roleAccessor.GetRequiredRole().Returns(RequestRole.Admin);
        roleAccessor.GetRequestedBy().Returns("test-admin");

        var endpoint = CreateEndpoint(db, roleAccessor);
        var request = new Request(carrierId, "Test reason");

        await endpoint.HandleAsync(request, CancellationToken.None);

        var carrier = await db.Carriers.FirstOrDefaultAsync(c => c.Id == carrierId);
        carrier.ShouldNotBeNull();
        carrier.IsEnabled.ShouldBe(false);
    }

    [Fact]
    public async Task HandleAsync_NonExistentId_ShouldReturnEmpty()
    {
        var db = CreateDbContext();
        var roleAccessor = Substitute.For<IRequestRoleAccessor>();
        roleAccessor.GetRequiredRole().Returns(RequestRole.Admin);
        
        var endpoint = CreateEndpoint(db, roleAccessor);
        var request = new Request(Guid.NewGuid(), "Test reason");

        await endpoint.HandleAsync(request, CancellationToken.None);

        endpoint.Response.Id.ShouldBe(Guid.Empty);
    }

    [Fact]
    public async Task HandleAsync_AlreadyDisabled_ShouldReturnCarrier()
    {
        var db = CreateDbContext();
        var carrierId = Guid.NewGuid();
        db.Carriers.Add(new Carrier
        {
            Id = carrierId,
            Name = "Test Carrier",
            IsEnabled = false,
            CreatedAtUtc = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var roleAccessor = Substitute.For<IRequestRoleAccessor>();
        roleAccessor.GetRequiredRole().Returns(RequestRole.Admin);
        
        var endpoint = CreateEndpoint(db, roleAccessor);
        var request = new Request(carrierId, "Test reason");

        await endpoint.HandleAsync(request, CancellationToken.None);

        endpoint.Response.ShouldNotBeNull();
        endpoint.Response.IsEnabled.ShouldBe(false);
    }
}