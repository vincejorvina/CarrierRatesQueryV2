using CarrierRatesQueryV2.Api.Features.Settlements.Update;
using FluentValidation.TestHelper;
using Xunit;

namespace CarrierRatesQueryV2.Tests.Features.Settlements.Update;

public class UpdateSettlementValidatorTests
{
    [Fact]
    public void Validate_ValidStatusPending_ShouldPass()
    {
        var validator = new Validator();
        var request = new Request(Guid.NewGuid(), "Pending");

        var result = validator.TestValidate(request);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_ValidStatusSettled_ShouldPass()
    {
        var validator = new Validator();
        var request = new Request(Guid.NewGuid(), "Settled");

        var result = validator.TestValidate(request);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_EmptyId_ShouldFail()
    {
        var validator = new Validator();
        var request = new Request(Guid.Empty, "Pending");

        var result = validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.Id);
    }

    [Fact]
    public void Validate_EmptyStatus_ShouldFail()
    {
        var validator = new Validator();
        var request = new Request(Guid.NewGuid(), "");

        var result = validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.Status);
    }

    [Fact]
    public void Validate_InvalidStatus_ShouldFail()
    {
        var validator = new Validator();
        var request = new Request(Guid.NewGuid(), "InvalidStatus");

        var result = validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.Status);
    }

    [Fact]
    public void Validate_CaseInsensitiveStatus_ShouldPass()
    {
        var validator = new Validator();
        var request = new Request(Guid.NewGuid(), "settled");

        var result = validator.TestValidate(request);

        result.ShouldNotHaveAnyValidationErrors();
    }
}