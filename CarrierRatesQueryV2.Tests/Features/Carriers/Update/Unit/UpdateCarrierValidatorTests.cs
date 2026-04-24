using System;
using System.Threading.Tasks;
using CarrierRatesQueryV2.Api.Features.Carriers.Update;
using CarrierRatesQueryV2.Data;
using CarrierRatesQueryV2.Data.Entities;
using FluentValidation.TestHelper;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CarrierRatesQueryV2.Tests.Features.Carriers.Update.Unit;

public class UpdateCarrierValidatorTests
{
    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"UpdateCarrierValidator_{Guid.NewGuid()}")
            .Options;
        return new AppDbContext(options);
    }

    [Fact]
    public async Task Validate_ValidRequest_ShouldPass()
    {
        var db = CreateDbContext();
        db.Carriers.Add(new Carrier { Id = Guid.NewGuid(), Name = "Existing", IsEnabled = true, CreatedAtUtc = DateTime.UtcNow });
        await db.SaveChangesAsync();

        var validator = new Validator(db);
        var request = new Request(Guid.NewGuid(), "New Name", null);

        var result = await validator.TestValidateAsync(request);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public async Task Validate_TooLongName_ShouldFail()
    {
        var db = CreateDbContext();
        var validator = new Validator(db);
        var request = new Request(Guid.NewGuid(), new string('a', 101), null);

        var result = await validator.TestValidateAsync(request);

        result.ShouldHaveValidationErrorFor(x => x.Name)
            .WithErrorMessage("Name must be less than 100 characters.");
    }

    [Fact]
    public async Task Validate_DuplicateName_ShouldFail()
    {
        var db = CreateDbContext();
        db.Carriers.Add(new Carrier { Id = Guid.NewGuid(), Name = "Existing Carrier", IsEnabled = true, CreatedAtUtc = DateTime.UtcNow });
        await db.SaveChangesAsync();

        var validator = new Validator(db);
        var request = new Request(Guid.NewGuid(), "Existing Carrier", null);

        var result = await validator.TestValidateAsync(request);

        result.ShouldHaveValidationErrorFor(x => x.Name)
            .WithErrorMessage("Name must be unique.");
    }

    [Fact]
    public async Task Validate_SameNameAsCurrent_ShouldPass()
    {
        var db = CreateDbContext();
        var existingId = Guid.NewGuid();
        db.Carriers.Add(new Carrier { Id = existingId, Name = "Test Carrier", IsEnabled = true, CreatedAtUtc = DateTime.UtcNow });
        await db.SaveChangesAsync();

        var validator = new Validator(db);
        var request = new Request(existingId, "Test Carrier", null);

        var result = await validator.TestValidateAsync(request);

        result.ShouldNotHaveAnyValidationErrors();
    }
}