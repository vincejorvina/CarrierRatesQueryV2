using CarrierRatesQueryV2.Api.Features.Carriers.Disable;
using CarrierRatesQueryV2.Api.Services;
using CarrierRatesQueryV2.Data;
using CarrierRatesQueryV2.Data.Entities;
using FluentValidation.TestHelper;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CarrierRatesQueryV2.Tests.Features.Carriers.Disable.Unit;

public class DisableCarrierValidatorTests
{
    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"DisableCarrierValidator_{Guid.NewGuid()}")
            .Options;
        return new AppDbContext(options);
    }

    [Fact]
    public async Task Validate_ValidRequest_ShouldPass()
    {
        var db = CreateDbContext();
        db.Carriers.Add(new Carrier
        {
            Id = Guid.NewGuid(),
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

        var carrierManagementService = new CarrierManagementService();
        var validator = new Validator(db, carrierManagementService);
        var carrierId = db.Carriers.First(c => c.Name == "Test Carrier").Id;
        var request = new Request(carrierId, "Test reason");

        var result = await validator.TestValidateAsync(request);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public async Task Validate_EmptyReason_ShouldFail()
    {
        var db = CreateDbContext();
        db.Carriers.Add(new Carrier
        {
            Id = Guid.NewGuid(),
            Name = "Test Carrier",
            IsEnabled = true,
            CreatedAtUtc = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var carrierManagementService = new CarrierManagementService();
        var validator = new Validator(db, carrierManagementService);
        var request = new Request(Guid.NewGuid(), "");

        var result = await validator.TestValidateAsync(request);

        result.ShouldHaveValidationErrorFor(x => x.Reason)
            .WithErrorMessage("Reason is required");
    }

    [Fact]
    public async Task Validate_LastEnabledCarrier_ShouldFail()
    {
        var db = CreateDbContext();
        var carrierId = Guid.NewGuid();
        db.Carriers.Add(new Carrier
        {
            Id = carrierId,
            Name = "Only Carrier",
            IsEnabled = true,
            CreatedAtUtc = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var carrierManagementService = new CarrierManagementService();
        var validator = new Validator(db, carrierManagementService);
        var request = new Request(carrierId, "Test reason");

        var result = await validator.TestValidateAsync(request);

        result.ShouldHaveValidationErrorFor(x => x.Id);
    }

    [Fact]
    public async Task Validate_PendingRequestsForOtherCarriers_ShouldFail()
    {
        var db = CreateDbContext();
        var carrierAId = Guid.NewGuid();
        var carrierBId = Guid.NewGuid();
        var carrierCId = Guid.NewGuid();

        db.Carriers.Add(new Carrier { Id = carrierAId, Name = "Carrier A", IsEnabled = true, CreatedAtUtc = DateTime.UtcNow });
        db.Carriers.Add(new Carrier { Id = carrierBId, Name = "Carrier B", IsEnabled = true, CreatedAtUtc = DateTime.UtcNow });
        db.Carriers.Add(new Carrier { Id = carrierCId, Name = "Carrier C", IsEnabled = true, CreatedAtUtc = DateTime.UtcNow });

        db.DisableRequests.Add(new DisableRequest
        {
            Id = Guid.NewGuid(),
            CarrierId = carrierBId,
            RequestedBy = "user1",
            Reason = "Disable B",
            Status = DisableRequestStatus.Pending,
            RequestedAtUtc = DateTime.UtcNow
        });
        db.DisableRequests.Add(new DisableRequest
        {
            Id = Guid.NewGuid(),
            CarrierId = carrierCId,
            RequestedBy = "user2",
            Reason = "Disable C",
            Status = DisableRequestStatus.Pending,
            RequestedAtUtc = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var carrierManagementService = new CarrierManagementService();
        var validator = new Validator(db, carrierManagementService);
        var request = new Request(carrierAId, "Test reason");

        var result = await validator.TestValidateAsync(request);

        result.ShouldHaveValidationErrorFor(x => x.Id);
    }

    [Fact]
    public async Task Validate_AlreadyDisabledCarrier_ShouldPass()
    {
        var db = CreateDbContext();
        db.Carriers.Add(new Carrier
        {
            Id = Guid.NewGuid(),
            Name = "Disabled Carrier",
            IsEnabled = false,
            CreatedAtUtc = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var carrierManagementService = new CarrierManagementService();
        var validator = new Validator(db, carrierManagementService);
        var carrierId = db.Carriers.First(c => c.Name == "Disabled Carrier").Id;
        var request = new Request(carrierId, "Test reason");

        var result = await validator.TestValidateAsync(request);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public async Task Validate_PendingShipment_ShouldFail()
    {
        var db = CreateDbContext();
        var carrierId = Guid.NewGuid();
        db.Carriers.Add(new Carrier { Id = carrierId, Name = "Test Carrier", IsEnabled = true, CreatedAtUtc = DateTime.UtcNow });
        db.Carriers.Add(new Carrier { Id = Guid.NewGuid(), Name = "Other Carrier", IsEnabled = true, CreatedAtUtc = DateTime.UtcNow });
        db.Shipments.Add(new Shipment
        {
            Id = Guid.NewGuid(),
            CarrierId = carrierId,
            Status = ShipmentStatus.Pending,
            CreatedAtUtc = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var carrierManagementService = new CarrierManagementService();
        var validator = new Validator(db, carrierManagementService);
        var request = new Request(carrierId, "Test reason");

        var result = await validator.TestValidateAsync(request);

        result.ShouldHaveValidationErrorFor(x => x.Id);
    }
}