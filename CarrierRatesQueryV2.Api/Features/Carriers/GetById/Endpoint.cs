using CarrierRatesQueryV2.Data;
using CarrierRatesQueryV2.Data.Entities;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace CarrierRatesQueryV2.Api.Features.Carriers.GetById;

public class EndpointSummary : Summary<Endpoint>
{
    public EndpointSummary()
    {
        Summary = "Get a carrier by ID";
        Description = "Retrieves a single carrier by its unique identifier, including its endpoints configuration.";
        Response(200, "Returns the carrier");
        Response(404, "Carrier with the specified ID was not found");
    }
}

public sealed record Request(Guid Id);

public sealed record EndpointDto(Guid Id, Guid CarrierId, string Operation, string Endpoint);

public sealed record Response(
    Guid Id,
    string Name,
    string Slug,
    bool IsEnabled,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc = null,
    List<EndpointDto>? Endpoints = null
);

public sealed class Endpoint(AppDbContext appDbContext) : Endpoint<Request, Response>
{
    public override void Configure()
    {
        Get("carriers/{id}");
        AllowAnonymous();
    }

    public override async Task HandleAsync(Request req, CancellationToken ct)
    {
        var carrier = await appDbContext.Carriers
            .Include(c => c.Endpoints)
            .FirstOrDefaultAsync(c => c.Id == req.Id, ct);

        if (carrier == null)
        {
            Response = null!;
            await Send.NotFoundAsync(ct);
            return;
        }

        var response = new Response(
            Id: carrier.Id,
            Name: carrier.Name,
            Slug: carrier.Slug,
            IsEnabled: carrier.IsEnabled,
            CreatedAtUtc: carrier.CreatedAtUtc,
            UpdatedAtUtc: carrier.UpdatedAtUtc,
            Endpoints: carrier.Endpoints?.Select(e => new EndpointDto(e.Id, e.CarrierId, e.Operation, e.Endpoint)).OrderBy(e => e.Operation).ToList() ?? []
        );

        await Send.OkAsync(response, ct);
    }
}