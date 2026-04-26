using CarrierRatesQueryV2.Api.Features.Shipments.Create;
using CarrierRatesQueryV2.Data;
using CarrierRatesQueryV2.Data.Entities;
using FluentValidation.TestHelper;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CarrierRatesQueryV2.Tests.Features.Shipments.Create;

public class CreateShipmentValidatorTests
{
    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"CreateShipmentValidator_{Guid.NewGuid()}")
            .Options;
        return new AppDbContext(options);
    }

    [Fact]
    public async Task Validate_ValidRequest_ShouldPass()
    {
        var db = CreateDbContext();
        var carrierId = Guid.NewGuid();
        db.Carriers.Add(new Carrier { Id = carrierId, Name = "Test Carrier", IsEnabled = true, CreatedAtUtc = DateTime.UtcNow });
        await db.SaveChangesAsync();

        var validator = new Validator(db);
        var request = new Request(carrierId);

        var result = await validator.TestValidateAsync(request);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public async Task Validate_EmptyCarrierId_ShouldFail()
    {
        var db = CreateDbContext();
        
        var validator = new Validator(db);
        var request = new Request(Guid.Empty);

        var result = await validator.TestValidateAsync(request);

        result.ShouldHaveValidationErrorFor(x => x.CarrierId);
    }

    [Fact]
    public async Task Validate_CarrierNotFound_ShouldFail()
    {
        var db = CreateDbContext();
        
        var validator = new Validator(db);
        var request = new Request(Guid.NewGuid());

        var result = await validator.TestValidateAsync(request);

        result.ShouldHaveValidationErrorFor(x => x.CarrierId);
    }
}