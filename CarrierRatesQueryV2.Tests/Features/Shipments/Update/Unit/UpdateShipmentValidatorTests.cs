using CarrierRatesQueryV2.Api.Features.Shipments.Update;
using CarrierRatesQueryV2.Tests.Infrastructure;
using FluentValidation.TestHelper;
using Xunit;

namespace CarrierRatesQueryV2.Tests.Features.Shipments.Update;

public class UpdateShipmentValidatorTests : ValidatorTestBase
{
    [Fact]
    public async Task Validate_ValidStatusPending_ShouldPass()
    {
        var (validator, _) = SetupValidator<Validator>();
        var request = new Request(Guid.NewGuid(), "Pending");

        var result = await validator.TestValidateAsync(request);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public async Task Validate_ValidStatusCompleted_ShouldPass()
    {
        var (validator, _) = SetupValidator<Validator>();
        var request = new Request(Guid.NewGuid(), "Completed");

        var result = await validator.TestValidateAsync(request);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public async Task Validate_EmptyId_ShouldFail()
    {
        var (validator, _) = SetupValidator<Validator>();
        var request = new Request(Guid.Empty, "Pending");

        var result = await validator.TestValidateAsync(request);

        result.ShouldHaveValidationErrorFor(x => x.Id);
    }

    [Fact]
    public async Task Validate_EmptyStatus_ShouldFail()
    {
        var (validator, _) = SetupValidator<Validator>();
        var request = new Request(Guid.NewGuid(), "");

        var result = await validator.TestValidateAsync(request);

        result.ShouldHaveValidationErrorFor(x => x.Status);
    }

    [Fact]
    public async Task Validate_InvalidStatus_ShouldFail()
    {
        var (validator, _) = SetupValidator<Validator>();
        var request = new Request(Guid.NewGuid(), "InvalidStatus");

        var result = await validator.TestValidateAsync(request);

        result.ShouldHaveValidationErrorFor(x => x.Status);
    }

    [Fact]
    public async Task Validate_CaseInsensitiveStatus_ShouldPass()
    {
        var (validator, _) = SetupValidator<Validator>();
        var request = new Request(Guid.NewGuid(), "completed");

        var result = await validator.TestValidateAsync(request);

        result.ShouldNotHaveAnyValidationErrors();
    }
}