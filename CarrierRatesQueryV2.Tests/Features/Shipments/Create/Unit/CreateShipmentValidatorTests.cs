using CarrierRatesQueryV2.Api.Features.Shipments.Create;
using CarrierRatesQueryV2.Api.Services;
using CarrierRatesQueryV2.Data.Entities;
using CarrierRatesQueryV2.Tests.Infrastructure;
using FluentValidation.TestHelper;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace CarrierRatesQueryV2.Tests.Features.Shipments.Create;

public class CreateShipmentValidatorTests : ValidatorTestBase
{
    [Fact]
    public async Task Validate_ValidRequest_ShouldPass()
    {
        // Arrange
        var (validator, db) = SetupValidator<Validator>(s =>
        {
            s.AddScoped<ICarrierManagementService, CarrierManagementService>();
        });
        var carrier = new Carrier { Id = Guid.NewGuid(), Name = "Test Carrier", IsEnabled = true, CreatedAtUtc = DateTime.UtcNow };
        db.Carriers.Add(carrier);
        await db.SaveChangesAsync();
        var carrierId = carrier.Id;

        var request = new Request(carrierId);

        // Act
        var result = await validator.TestValidateAsync(request);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public async Task Validate_EmptyCarrierId_ShouldFail()
    {
        // Arrange
        var (validator, _) = SetupValidator<Validator>();
        
        var request = new Request(Guid.Empty);

        // Act
        var result = await validator.TestValidateAsync(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.CarrierId);
    }

    [Fact]
    public async Task Validate_CarrierNotFound_ShouldFail()
    {
        // Arrange
        var (validator, _) = SetupValidator<Validator>();
        
        var request = new Request(Guid.NewGuid());

        // Act
        var result = await validator.TestValidateAsync(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.CarrierId);
    }
}