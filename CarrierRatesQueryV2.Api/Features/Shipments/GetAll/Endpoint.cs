using CarrierRatesQueryV2.Data;
using CarrierRatesQueryV2.Data.Entities;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace CarrierRatesQueryV2.Api.Features.Shipments.GetAll;

public class EndpointSummary : Summary<Endpoint>
{
    public EndpointSummary()
    {
        Summary = "Get all shipments";
        Description = "Retrieves all shipments in the system.";
        Response(200, "Returns a list of shipments");
    }
}

public sealed record Response(Guid Id, Guid CarrierId, string Status, DateTime CreatedAtUtc);

public sealed class Endpoint(AppDbContext appDbContext) : EndpointWithoutRequest<List<Response>>
{
    public override void Configure()
    {
        Get("shipments");
        AllowAnonymous();
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var shipments = await appDbContext.Shipments
            .OrderByDescending(s => s.CreatedAtUtc)
            .ToListAsync(ct);

        var response = shipments.Select(s => new Response(s.Id, s.CarrierId, s.Status.ToString(), s.CreatedAtUtc)).ToList();
        await Send.OkAsync(response, ct);
    }
}