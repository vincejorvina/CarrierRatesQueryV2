using CarrierRatesQueryV2.Api.Features.Carriers.Update;
using CarrierRatesQueryV2.Data;
using CarrierRatesQueryV2.Data.Entities;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using Xunit;

namespace CarrierRatesQueryV2.Tests.Features.Carriers.Update.Unit;

public class UpdateCarrierHandlerTests
{
    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"UpdateCarrier_{Guid.NewGuid()}")
            .Options;
        return new AppDbContext(options);
    }

    private static Endpoint CreateEndpoint(AppDbContext db)
    {
        return Factory.Create<Endpoint>(db);
    }

    [Fact]
    public async Task HandleAsync_UpdateName_ShouldUpdateCarrier()
    {
        var db = CreateDbContext();
        var carrierId = Guid.NewGuid();
        db.Carriers.Add(new Carrier
        {
            Id = carrierId,
            Name = "Old Name",
            IsEnabled = true,
            CreatedAtUtc = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var endpoint = CreateEndpoint(db);
        var request = new Request(carrierId, "New Name", null);

        await endpoint.HandleAsync(request, CancellationToken.None);

        var updatedCarrier = await db.Carriers.FirstOrDefaultAsync(c => c.Id == carrierId);
        updatedCarrier.ShouldNotBeNull();
        updatedCarrier.Name.ShouldBe("New Name");
        updatedCarrier.UpdatedAtUtc.ShouldNotBeNull();
    }

    [Fact]
    public async Task HandleAsync_EnableDisabledCarrier_ShouldUpdateCarrier()
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

        var endpoint = CreateEndpoint(db);
        var request = new Request(carrierId, null, true);

        await endpoint.HandleAsync(request, CancellationToken.None);

        var updatedCarrier = await db.Carriers.FirstOrDefaultAsync(c => c.Id == carrierId);
        updatedCarrier.ShouldNotBeNull();
        updatedCarrier.IsEnabled.ShouldBe(true);
    }

    [Fact]
    public async Task HandleAsync_NonExistentId_ShouldReturnNotFound()
    {
        var db = CreateDbContext();
        var endpoint = CreateEndpoint(db);
        var request = new Request(Guid.NewGuid(), "New Name", null);

        await endpoint.HandleAsync(request, CancellationToken.None);

        endpoint.Response.ShouldBeNull();
    }

    [Fact]
    public async Task HandleAsync_DisableEnabledCarrier_ShouldThrowError()
    {
        var db = CreateDbContext();
        var carrierId = Guid.NewGuid();
        db.Carriers.Add(new Carrier
        {
            Id = carrierId,
            Name = "Old Name",
            IsEnabled = true,
            CreatedAtUtc = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var endpoint = CreateEndpoint(db);
        var request = new Request(carrierId, "New Name", false);

        await Should.ThrowAsync<FastEndpoints.ValidationFailureException>(async () =>
            await endpoint.HandleAsync(request, CancellationToken.None));

        var updatedCarrier = await db.Carriers.FirstOrDefaultAsync(c => c.Id == carrierId);
        updatedCarrier.ShouldNotBeNull();
        updatedCarrier.Name.ShouldBe("Old Name");
        updatedCarrier.IsEnabled.ShouldBe(true);
    }
}
