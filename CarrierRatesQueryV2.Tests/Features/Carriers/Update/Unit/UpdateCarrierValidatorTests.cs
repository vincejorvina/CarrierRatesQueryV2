using CarrierRatesQueryV2.Api.Features.Carriers.Update;
using CarrierRatesQueryV2.Data;
using CarrierRatesQueryV2.Data.Entities;
using CarrierRatesQueryV2.Tests.Infrastructure;
using FluentValidation.TestHelper;
using Xunit;

namespace CarrierRatesQueryV2.Tests.Features.Carriers.Update.Unit;

public class UpdateCarrierValidatorTests : ValidatorTestBase
{
    [Fact]
    public async Task Validate_ValidRequest_ShouldPass()
    {
        // Arrange
        var (validator, _) = SetupValidator<Validator>();

        var request = new Request(Guid.NewGuid(), "New Name", null);

        // Act
        var result = await validator.TestValidateAsync(request);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public async Task Validate_TooLongName_ShouldFail()
    {
        // Arrange
        var (validator, _) = SetupValidator<Validator>();
        var request = new Request(Guid.NewGuid(), new string('a', 101), null);

        // Act
        var result = await validator.TestValidateAsync(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Name)
            .WithErrorMessage("Name must be less than 100 characters.");
    }

    [Fact]
    public async Task Validate_DuplicateName_ShouldFail()
    {
        // Arrange
        var (validator, db) = SetupValidator<Validator>();
        db.Carriers.Add(new Carrier { Id = Guid.NewGuid(), Name = "Existing Carrier", IsEnabled = true, CreatedAtUtc = DateTime.UtcNow });
        await db.SaveChangesAsync();

        var request = new Request(Guid.NewGuid(), "Existing Carrier", null);

        // Act
        var result = await validator.TestValidateAsync(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Name)
            .WithErrorMessage("Name must be unique.");
    }

    [Fact]
    public async Task Validate_SameNameAsCurrent_ShouldPass()
    {
        // Arrange
        var (validator, db) = SetupValidator<Validator>();
        var existingId = Guid.NewGuid();
        db.Carriers.Add(new Carrier { Id = existingId, Name = "Test Carrier", IsEnabled = true, CreatedAtUtc = DateTime.UtcNow });
        await db.SaveChangesAsync();

        var request = new Request(existingId, "Test Carrier", null);

        // Act
        var result = await validator.TestValidateAsync(request);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }
}
