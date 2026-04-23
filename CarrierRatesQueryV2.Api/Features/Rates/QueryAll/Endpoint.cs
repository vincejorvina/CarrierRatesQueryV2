using CarrierRatesQueryV2.Core.Contracts.Rates;
using CarrierRatesQueryV2.Core.Interfaces.Rates;
using CarrierRatesQueryV2.Data;
using FastEndpoints;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace CarrierRatesQueryV2.Api.Features.Rates.QueryAll;

public sealed class Endpoint(
    AppDbContext appDbContext,
    ICarrierRateStrategyResolver carrierRateStrategyResolver) : Endpoint<Request, List<RateQuoteResponse>>
{
    public override void Configure()
    {
        Post("rates");
        AllowAnonymous();
    }

    public override async Task HandleAsync(Request req, CancellationToken ct)
    {
        var carriers = await appDbContext.Carriers
            .Include(x => x.Endpoints)
            .Where(x => x.IsEnabled)
            .OrderBy(x => x.Name)
            .ToListAsync(ct);

        var query = req.ToRateQuery();
        var results = new List<RateQuoteResponse>();

        foreach (var carrier in carriers)
        {
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
                continue;
            }

            var quote = await strategy.TryGetRatesAsync(carrierContext, query, ct);
            if (quote is null)
            {
                continue;
            }

            results.Add(RateQuoteResponse.FromQuote(quote));
        }

        await Send.OkAsync(results, ct);
    }
}

public sealed record Request(LocationRequest Origin, LocationRequest Destination, PackageRequest Package)
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

public sealed record RateQuoteResponse(string Carrier, IReadOnlyList<RateOptionResponse> RateOptions)
{
    public static RateQuoteResponse FromQuote(ShippingRateQuote quote)
    {
        return new RateQuoteResponse(
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
