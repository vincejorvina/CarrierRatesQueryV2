using CarrierRatesQueryV2.Api.Features.CarrierEndpoints.Create;
using CarrierRatesQueryV2.Data.Entities;
using CarrierRatesQueryV2.Tests.Infrastructure;
using FluentValidation.TestHelper;
using Xunit;

namespace CarrierRatesQueryV2.Tests.Features.CarrierEndpoints.Create.Unit;

public class CreateCarrierEndpointValidatorTests : ValidatorTestBase
{
    [Fact]
    public async Task Validate_ValidRequest_ShouldPass()
    {
        var (validator, _) = SetupValidator<Validator>();
        var request = new Request(Guid.NewGuid(), "Rates", "https://api.test.com/rates");

        var result = await validator.TestValidateAsync(request);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public async Task Validate_EmptyOperation_ShouldFail()
    {
        var (validator, _) = SetupValidator<Validator>();
        var request = new Request(Guid.NewGuid(), "", "https://api.test.com/rates");

        var result = await validator.TestValidateAsync(request);

        result.ShouldHaveValidationErrorFor(x => x.Operation);
    }

    [Fact]
    public async Task Validate_EmptyEndpoint_ShouldFail()
    {
        var (validator, _) = SetupValidator<Validator>();
        var request = new Request(Guid.NewGuid(), "Rates", "");

        var result = await validator.TestValidateAsync(request);

        result.ShouldHaveValidationErrorFor(x => x.Endpoint);
    }
}