using CarrierRatesQueryV2.Data;
using CarrierRatesQueryV2.Data.Entities;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace CarrierRatesQueryV2.Api.Features.Carriers.Enable;

public class EndpointSummary : Summary<Endpoint>
{
    public EndpointSummary()
    {
        Summary = "Enable a carrier";
        Description = "Enables a previously disabled carrier, allowing it to receive rate queries.";
        Response(200, "Carrier has been enabled", example: new Response(
            Guid.Empty,
            "FedEx Ground",
            "fedex-ground",
            true,
            DateTime.UtcNow,
            DateTime.UtcNow,
            null));
        Response(404, "Carrier with the specified ID was not found");
    }
}

public sealed record Request(Guid Id);

public sealed record Response(
    Guid Id,
    string Name,
    string Slug,
    bool IsEnabled,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc = null,
    List<CarrierRatesQueryV2.Data.Entities.CarrierEndpoint>? Endpoints = null
);

public sealed class Endpoint(AppDbContext appDbContext) : Endpoint<Request, Response>
{
    public override void Configure()
    {
        Patch("carriers/{id}/enable");
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

        carrier.IsEnabled = true;
        carrier.UpdatedAtUtc = DateTime.UtcNow;

        await appDbContext.SaveChangesAsync(ct);

        Response = new Response(
            Id: carrier.Id,
            Name: carrier.Name,
            Slug: carrier.Slug,
            IsEnabled: carrier.IsEnabled,
            CreatedAtUtc: carrier.CreatedAtUtc,
            UpdatedAtUtc: carrier.UpdatedAtUtc,
            Endpoints: carrier.Endpoints?.OrderBy(e => e.Operation).ToList() ?? []
        );

        await Send.OkAsync(ct);
    }
}