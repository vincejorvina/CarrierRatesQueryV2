using CarrierRatesQueryV2.Data;
using CarrierRatesQueryV2.Data.Entities;
using FastEndpoints;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace CarrierRatesQueryV2.Api.Features.CarrierEndpoints.Update;

public sealed class Endpoint(AppDbContext appDbContext) : Endpoint<Request, Response>
{
    public override void Configure()
    {
        Put("carriers/{carrierId}/endpoints/{endpointId}");
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

        endpoint.Operation = req.Operation;
        endpoint.Endpoint = req.Endpoint;

        await appDbContext.SaveChangesAsync(ct);

        Response = new Response(endpoint.Id, endpoint.CarrierId, endpoint.Operation, endpoint.Endpoint);
        await Send.OkAsync(Response, ct);
    }
}

public sealed record Request(Guid CarrierId, Guid EndpointId, string Operation, string Endpoint);

public sealed class Validator : Validator<Request>
{
    public Validator()
    {
        RuleFor(x => x.Operation).NotEmpty();
        RuleFor(x => x.Endpoint).NotEmpty();
    }
}

public sealed record Response(Guid Id, Guid CarrierId, string Operation, string Endpoint);