using CarrierRatesQueryV2.Api.Features.Carriers.Disable;
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

        var validator = new Validator(db);
        var CarrierId = db.Carriers.First(c => c.Name == "Test Carrier").Id;
        var request = new Request(CarrierId, "Test reason");

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

        var validator = new Validator(db);
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

        var validator = new Validator(db);
        var request = new Request(carrierId, "Test reason");

        var result = await validator.TestValidateAsync(request);

        result.ShouldHaveValidationErrorFor(x => x.Id)
            .WithErrorMessage("Cannot disable the only enabled carrier.");
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

        var validator = new Validator(db);
        var CarrierId = db.Carriers.First(c => c.Name == "Disabled Carrier").Id;
        var request = new Request(CarrierId, "Test reason");

        var result = await validator.TestValidateAsync(request);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public async Task Validate_NonExistentCarrier_ShouldPass()
    {
        var db = CreateDbContext();
        var validator = new Validator(db);
        var request = new Request(Guid.NewGuid(), "Test reason");

        var result = await validator.TestValidateAsync(request);

        result.ShouldNotHaveAnyValidationErrors();
    }
}