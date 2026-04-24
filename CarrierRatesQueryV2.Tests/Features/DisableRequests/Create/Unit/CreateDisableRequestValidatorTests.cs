using CarrierRatesQueryV2.Api.Features.DisableRequests.Create;
using FluentValidation.TestHelper;
using Xunit;

namespace CarrierRatesQueryV2.Tests.Features.DisableRequests.Create.Unit;

public class CreateDisableRequestValidatorTests
{
    [Fact]
    public void Validate_ValidRequest_ShouldPass()
    {
        var validator = new Validator();
        var request = new Request(Guid.NewGuid(), "Contract termination");

        var result = validator.TestValidate(request);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_EmptyReason_ShouldFail()
    {
        var validator = new Validator();
        var request = new Request(Guid.NewGuid(), "");

        var result = validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.Reason);
    }
}