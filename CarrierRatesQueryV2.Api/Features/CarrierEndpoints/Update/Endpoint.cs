using CarrierRatesQueryV2.Data;
using CarrierRatesQueryV2.Data.Entities;
using FastEndpoints;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace CarrierRatesQueryV2.Api.Features.CarrierEndpoints.Update;

public class EndpointSummary : Summary<Endpoint>
{
    public EndpointSummary()
    {
        Summary = "Update a carrier endpoint";
        Description = "Updates an existing endpoint configuration for a carrier.";
        ExampleRequest = new Request(Guid.Empty, Guid.Empty, "rate", "https://api.carrier.com/v2/rates");
        Response(200, "Endpoint updated successfully", example: new Response(
            Guid.Empty,
            Guid.Empty,
            "rate",
            "https://api.carrier.com/v2/rates"));
        Response(400, "Validation failed - operation or endpoint URL is empty");
        Response(404, "Carrier or endpoint with the specified IDs was not found");
    }
}

public sealed record Request(Guid CarrierId, Guid EndpointId, string Operation, string Endpoint);

public sealed record Response(Guid Id, Guid CarrierId, string Operation, string Endpoint);

public sealed class Endpoint(AppDbContext appDbContext) : Endpoint<Request, Response>
{
    public override void Configure()
    {
        Put("carriers/{carrierId}/endpoints/{endpointId}");
        AllowAnonymous();
    }

    public override async Task HandleAsync(Request req, CancellationToken ct)
    {
        var carrierExists = await appDbContext.Carriers.AnyAsync(c => c.Id == req.CarrierId, ct);
        if (!carrierExists)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        var endpoint = await appDbContext.CarrierEndpoints
            .AsTracking()
            .FirstOrDefaultAsync(e => e.CarrierId == req.CarrierId && e.Id == req.EndpointId, ct);

        if (endpoint == null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        endpoint.Operation = req.Operation;
        endpoint.Endpoint = req.Endpoint;

        await appDbContext.SaveChangesAsync(ct);

        var response = new Response(endpoint.Id, endpoint.CarrierId, endpoint.Operation, endpoint.Endpoint);
        await Send.OkAsync(response, ct);
    }
}

public sealed class Validator : Validator<Request>
{
    public Validator()
    {
        RuleFor(x => x.Operation).NotEmpty();
        RuleFor(x => x.Endpoint).NotEmpty();
    }
}