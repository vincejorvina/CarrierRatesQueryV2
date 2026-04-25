using CarrierRatesQueryV2.Data;
using CarrierRatesQueryV2.Data.Entities;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace CarrierRatesQueryV2.Api.Features.CarrierEndpoints.GetAll;

public class EndpointSummary : Summary<Endpoint>
{
    public EndpointSummary()
    {
        Summary = "Get all endpoints for a carrier";
        Description = "Retrieves all endpoint configurations for a specific carrier.";
        Response(200, "Returns a list of carrier endpoints");
        Response(404, "Carrier with the specified ID was not found");
    }
}

public sealed record Request(Guid CarrierId);

public sealed record Response(Guid Id, Guid CarrierId, string Operation, string Endpoint);

public sealed class Endpoint(AppDbContext appDbContext) : Endpoint<Request, List<Response>>
{
    public override void Configure()
    {
        Get("carriers/{carrierId}/endpoints");
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

        var endpoints = await appDbContext.CarrierEndpoints
            .Where(e => e.CarrierId == req.CarrierId)
            .OrderBy(e => e.Operation)
            .ToListAsync(ct);

        var response = endpoints.Select(e => new Response(e.Id, e.CarrierId, e.Operation, e.Endpoint)).ToList();
        await Send.OkAsync(response, ct);
    }
}