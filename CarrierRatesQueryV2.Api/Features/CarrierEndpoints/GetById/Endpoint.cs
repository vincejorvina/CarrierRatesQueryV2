using CarrierRatesQueryV2.Data;
using CarrierRatesQueryV2.Data.Entities;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace CarrierRatesQueryV2.Api.Features.CarrierEndpoints.GetById;

public class EndpointSummary : Summary<Endpoint>
{
    public EndpointSummary()
    {
        Summary = "Get a carrier endpoint by ID";
        Description = "Retrieves a single endpoint configuration for a carrier by its unique identifier.";
        Response(200, "Returns the carrier endpoint", example: new Response(
            Guid.Empty,
            Guid.Empty,
            "rate",
            "https://api.carrier.com/rates"));
        Response(404, "Carrier or endpoint with the specified IDs was not found");
    }
}

public sealed record Request(Guid CarrierId, Guid EndpointId);

public sealed record Response(Guid Id, Guid CarrierId, string Operation, string Endpoint);

public sealed class Endpoint(AppDbContext appDbContext) : Endpoint<Request, Response>
{
    public override void Configure()
    {
        Get("carriers/{carrierId}/endpoints/{endpointId}");
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
            .FirstOrDefaultAsync(e => e.CarrierId == req.CarrierId && e.Id == req.EndpointId, ct);

        if (endpoint == null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        Response = new Response(endpoint.Id, endpoint.CarrierId, endpoint.Operation, endpoint.Endpoint);
        await Send.OkAsync(ct);
    }
}