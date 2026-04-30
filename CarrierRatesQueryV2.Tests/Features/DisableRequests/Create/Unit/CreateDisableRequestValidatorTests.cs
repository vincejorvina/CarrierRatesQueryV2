using CarrierRatesQueryV2.Api.Features.DisableRequests.Create;
using CarrierRatesQueryV2.Api.Services;
using CarrierRatesQueryV2.Data.Entities;
using CarrierRatesQueryV2.Tests.Infrastructure;
using FluentValidation.TestHelper;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace CarrierRatesQueryV2.Tests.Features.DisableRequests.Create.Unit;

public class CreateDisableRequestValidatorTests : ValidatorTestBase
{
    [Fact]
    public async Task Validate_ValidRequest_ShouldPass()
    {
        // Arrange
        var (validator, db) = SetupValidator<Validator>(s =>
        {
            s.AddScoped<ICarrierManagementService, CarrierManagementService>();
        });
        var carrierId = Guid.NewGuid();
        db.Carriers.Add(new Carrier { Id = carrierId, Name = "Test Carrier", IsEnabled = true, CreatedAtUtc = DateTime.UtcNow });
        db.Carriers.Add(new Carrier { Id = Guid.NewGuid(), Name = "Other Carrier", IsEnabled = true, CreatedAtUtc = DateTime.UtcNow });
        await db.SaveChangesAsync();

        var request = new Request(carrierId, "Contract termination");

        // Act
        var result = await validator.TestValidateAsync(request);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public async Task Validate_EmptyReason_ShouldFail()
    {
        // Arrange
        var (validator, db) = SetupValidator<Validator>(s =>
        {
            s.AddScoped<ICarrierManagementService, CarrierManagementService>();
        });
        db.Carriers.Add(new Carrier { Id = Guid.NewGuid(), Name = "Test Carrier", IsEnabled = true, CreatedAtUtc = DateTime.UtcNow });
        await db.SaveChangesAsync();

        var request = new Request(Guid.NewGuid(), "");

        // Act
        var result = await validator.TestValidateAsync(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Reason);
    }

    [Fact]
    public async Task Validate_LastEnabledCarrier_ShouldFail()
    {
        // Arrange
        var (validator, db) = SetupValidator<Validator>(s =>
        {
            s.AddScoped<ICarrierManagementService, CarrierManagementService>();
        });
        var carrierId = Guid.NewGuid();
        db.Carriers.Add(new Carrier { Id = carrierId, Name = "Only Carrier", IsEnabled = true, CreatedAtUtc = DateTime.UtcNow });
        await db.SaveChangesAsync();

        var request = new Request(carrierId, "Test reason");

        // Act
        var result = await validator.TestValidateAsync(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.CarrierId);
    }

    [Fact]
    public async Task Validate_PendingRequestsForOtherCarriers_ShouldFail()
    {
        // Arrange
        var (validator, db) = SetupValidator<Validator>(s =>
        {
            s.AddScoped<ICarrierManagementService, CarrierManagementService>();
        });
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

        var request = new Request(carrierAId, "Test reason");

        // Act
        var result = await validator.TestValidateAsync(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.CarrierId);
    }

    [Fact]
    public async Task Validate_PendingShipment_ShouldFail()
    {
        // Arrange
        var (validator, db) = SetupValidator<Validator>(s =>
        {
            s.AddScoped<ICarrierManagementService, CarrierManagementService>();
        });
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

        var request = new Request(carrierId, "Test reason");

        // Act
        var result = await validator.TestValidateAsync(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.CarrierId);
    }

    [Fact]
    public async Task Validate_PendingSettlement_ShouldFail()
    {
        // Arrange
        var (validator, db) = SetupValidator<Validator>(s =>
        {
            s.AddScoped<ICarrierManagementService, CarrierManagementService>();
        });
        var carrierId = Guid.NewGuid();
        db.Carriers.Add(new Carrier { Id = carrierId, Name = "Test Carrier", IsEnabled = true, CreatedAtUtc = DateTime.UtcNow });
        db.Carriers.Add(new Carrier { Id = Guid.NewGuid(), Name = "Other Carrier", IsEnabled = true, CreatedAtUtc = DateTime.UtcNow });
        db.CarrierFinancialSettlements.Add(new CarrierFinancialSettlement
        {
            Id = Guid.NewGuid(),
            CarrierId = carrierId,
            Status = CarrierFinancialSettlementStatus.Pending,
            CreatedAtUtc = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var request = new Request(carrierId, "Test reason");

        // Act
        var result = await validator.TestValidateAsync(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.CarrierId);
    }

    [Fact]
    public async Task Validate_AlreadyDisabledCarrier_ShouldPass()
    {
        // Arrange
        var (validator, db) = SetupValidator<Validator>(s =>
        {
            s.AddScoped<ICarrierManagementService, CarrierManagementService>();
        });
        var carrierId = Guid.NewGuid();
        db.Carriers.Add(new Carrier { Id = carrierId, Name = "Test Carrier", IsEnabled = false, CreatedAtUtc = DateTime.UtcNow });
        await db.SaveChangesAsync();

        var request = new Request(carrierId, "Test reason");

        // Act
        var result = await validator.TestValidateAsync(request);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }
}
