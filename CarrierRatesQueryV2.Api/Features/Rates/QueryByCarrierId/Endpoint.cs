using CarrierRatesQueryV2.Core.Contracts.Rates;
using CarrierRatesQueryV2.Core.Interfaces.Rates;
using CarrierRatesQueryV2.Data;
using FastEndpoints;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace CarrierRatesQueryV2.Api.Features.Rates.QueryByCarrierId;

 public class EndpointSummary : Summary<Endpoint>
 {
     public EndpointSummary()
     {
         Summary = "Query rates by carrier ID";
         Description = "Queries shipping rates for a specific carrier by its ID. Returns rate quotes from the specified carrier if it is enabled and can provide rates for the given shipment details. (Expects SI units: weight in kilograms, dimensions in centimeters)";
         ExampleRequest = new Request(
             Guid.Parse("00000000-0000-0000-0000-000000000000"),
             new LocationRequest("90210", "US"),
             new LocationRequest("10001", "US"),
             new PackageRequest(10m, new PackageDimensionsRequest(10m, 8m, 6m)));
         Response(200, "Returns available shipping rates from the specified carrier");
         Response(400, "Validation failed - missing or invalid request fields");
         Response(404, "Carrier with the specified ID was not found");
         Response(409, "Conflict - carrier is not enabled");
      }
  }

public sealed record Request(Guid CarrierId, LocationRequest Origin, LocationRequest Destination, PackageRequest Package)
{
    public RateQuery ToRateQuery()
    {
        return new RateQuery(
            Origin: new Location(Origin.PostalCode.Trim(), Origin.CountryCode.Trim()),
            Destination: new Location(Destination.PostalCode.Trim(), Destination.CountryCode.Trim()),
            Package: new Package(
                Weight: Package.Weight,
                Dimensions: new PackageDimensions(
                    Length: Package.Dimensions.Length,
                    Width: Package.Dimensions.Width,
                    Height: Package.Dimensions.Height)));
    }
}

public sealed record LocationRequest(string PostalCode, string CountryCode);

public sealed record PackageRequest(decimal Weight, PackageDimensionsRequest Dimensions);

public sealed record PackageDimensionsRequest(decimal Length, decimal Width, decimal Height);

public sealed record Response(string Carrier, IReadOnlyList<RateOptionResponse> RateOptions)
{
    public static Response FromQuote(ShippingRateQuote quote)
    {
        return new Response(
            quote.Carrier,
            quote.RateOptions
                .Select(x => new RateOptionResponse(
                    x.ServiceName,
                    x.EstimatedDelivery,
                    new MoneyResponse(x.Price.Amount, x.Price.Currency)))
                .ToList());
    }
}

public sealed record RateOptionResponse(string ServiceName, DateTime EstimatedDelivery, MoneyResponse Price);

public sealed record MoneyResponse(decimal Amount, string Currency);

public sealed class Endpoint(
    AppDbContext appDbContext,
    ICarrierRateStrategyResolver carrierRateStrategyResolver) : Endpoint<Request, Response>
{
    public override void Configure()
    {
        Post("rates/carrier/{carrierId}");
        AllowAnonymous();
    }

    public override async Task HandleAsync(Request req, CancellationToken ct)
    {
        var carrier = await appDbContext.Carriers
            .Include(x => x.Endpoints)
            .FirstOrDefaultAsync(c => c.Id == req.CarrierId, ct);

        if (carrier == null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        if (!carrier.IsEnabled)
        {
            ThrowError("Carrier is disabled", 409);
            return;
        }

        var carrierContext = new CarrierContext(
            Id: carrier.Id,
            Name: carrier.Name,
            Slug: carrier.Slug,
            UpdatedAtUtc: carrier.UpdatedAtUtc,
            Endpoints: carrier.Endpoints
                .Select(x => new CarrierEndpointConfig(x.Operation, x.Endpoint))
                .ToList());

        if (!carrierRateStrategyResolver.TryResolve(carrierContext.Slug, out var strategy))
        {
            ThrowError("No rate strategy found for carrier", 409);
            return;
        }

        var query = req.ToRateQuery();
        var quote = await strategy.TryGetRatesAsync(carrierContext, query, ct);

        if (quote == null)
        {
            ThrowError("No rates available for carrier", 409);
            return;
        }

        var response = Response.FromQuote(quote);
        await Send.OkAsync(response, ct);
    }
}

public sealed class Validator : Validator<Request>
{
    public Validator()
    {
        RuleFor(x => x.Origin).NotNull();
        RuleFor(x => x.Destination).NotNull();
        RuleFor(x => x.Package).NotNull();
        RuleFor(x => x.Package.Dimensions).NotNull();
        RuleFor(x => x.Origin.PostalCode).NotEmpty();
        RuleFor(x => x.Origin.CountryCode).NotEmpty();
        RuleFor(x => x.Destination.PostalCode).NotEmpty();
        RuleFor(x => x.Destination.CountryCode).NotEmpty();
        RuleFor(x => x.Package.Weight).GreaterThan(0m);
        RuleFor(x => x.Package.Dimensions.Length).GreaterThan(0m);
        RuleFor(x => x.Package.Dimensions.Width).GreaterThan(0m);
        RuleFor(x => x.Package.Dimensions.Height).GreaterThan(0m);
    }
}