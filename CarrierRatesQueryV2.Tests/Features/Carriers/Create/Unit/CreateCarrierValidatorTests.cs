using CarrierRatesQueryV2.Api.Features.Carriers.Create;
using CarrierRatesQueryV2.Data;
using CarrierRatesQueryV2.Data.Entities;
using FluentValidation.TestHelper;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CarrierRatesQueryV2.Tests.Features.Carriers.Create.Unit;

public class CreateCarrierValidatorTests
{
    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"CreateCarrierHandler_{Guid.NewGuid()}")
            .Options;
        return new AppDbContext(options);
    }

    [Fact]
    public async Task Validate_ValidRequest_ShouldPass()
    {
        // Arrange
        var db = CreateDbContext();
        var validator = new Validator(db);
        var request = new Request("Test Carrier", true);

        // Act
        var result = await validator.TestValidateAsync(request);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public async Task Validate_EmptyName_ShouldFail()
    {
        // Arrange
        var db = CreateDbContext();
        var validator = new Validator(db);
        var request = new Request("", true);

        // Act
        var result = await validator.TestValidateAsync(request);

        // Assert
        result.ShouldHaveValidationErrorFor(r => r.Name)
            .WithErrorMessage("Name is required.");
    }

    [Fact]
    public async Task Validate_TooLongName_ShouldFail()
    {
        // Arrange
        var db = CreateDbContext();
        var validator = new Validator(db);
        var request = new Request(new string('a', 101), true);

        // Act
        var result = await validator.TestValidateAsync(request);

        // Assert
        result.ShouldHaveValidationErrorFor(r => r.Name)
            .WithErrorMessage("Name must be less than 100 characters.");
    }

    [Fact]
    public async Task Validate_DuplicateName_ShouldFail()
    {
        // Arrange
        var db = CreateDbContext();
        db.Carriers.Add(new Carrier { Id = Guid.NewGuid(), Name = "Existing Carrier", IsEnabled = true, CreatedAtUtc = DateTime.UtcNow });
        await db.SaveChangesAsync();

        var validator = new Validator(db);
        var request = new Request("Existing Carrier", true);

        // Act
        var result = await validator.TestValidateAsync(request);

        // Assert
        result.ShouldHaveValidationErrorFor(r => r.Name)
            .WithErrorMessage("Name must be unique.");
    }

    [Fact]
    public async Task Validate_ValidRequestWithExistingCarrier_ShouldPass()
    {
        // Arrange
        var db = CreateDbContext();
        db.Carriers.Add(new Carrier { Id = Guid.NewGuid(), Name = "Other Carrier", IsEnabled = true, CreatedAtUtc = DateTime.UtcNow });
        await db.SaveChangesAsync();

        var validator = new Validator(db);
        var request = new Request("New Carrier", true);

        // Act
        var result = await validator.TestValidateAsync(request);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }
}
