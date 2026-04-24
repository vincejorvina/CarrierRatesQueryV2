using CarrierRatesQueryV2.Api.Features.CarrierEndpoints.Update;
using FluentValidation.TestHelper;
using Xunit;

namespace CarrierRatesQueryV2.Tests.Features.CarrierEndpoints.Update.Unit;

public class UpdateCarrierEndpointValidatorTests
{
    [Fact]
    public void Validate_ValidRequest_ShouldPass()
    {
        var validator = new Validator();
        var request = new Request(Guid.NewGuid(), Guid.NewGuid(), "Rates", "https://api.test.com/rates");

        var result = validator.TestValidate(request);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_EmptyOperation_ShouldFail()
    {
        var validator = new Validator();
        var request = new Request(Guid.NewGuid(), Guid.NewGuid(), "", "https://api.test.com/rates");

        var result = validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.Operation);
    }

    [Fact]
    public void Validate_EmptyEndpoint_ShouldFail()
    {
        var validator = new Validator();
        var request = new Request(Guid.NewGuid(), Guid.NewGuid(), "Rates", "");

        var result = validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.Endpoint);
    }
}