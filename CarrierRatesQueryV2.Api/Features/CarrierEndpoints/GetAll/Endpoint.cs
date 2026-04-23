using CarrierRatesQueryV2.Data;
using CarrierRatesQueryV2.Data.Entities;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace CarrierRatesQueryV2.Api.Features.CarrierEndpoints.GetAll;

public sealed class Endpoint(AppDbContext appDbContext) : Endpoint<Request, List<Response>>
{
    public override void Configure()
    {
        Get("carriers/{carrierId}/endpoints");
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

        Response = endpoints.Select(e => new Response(e.Id, e.CarrierId, e.Operation, e.Endpoint)).ToList();
        await Send.OkAsync(ct);
    }
}

public sealed record Request(Guid CarrierId);

public sealed record Response(Guid Id, Guid CarrierId, string Operation, string Endpoint);