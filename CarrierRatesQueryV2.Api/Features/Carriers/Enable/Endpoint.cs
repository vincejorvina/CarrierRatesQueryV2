using CarrierRatesQueryV2.Data;
using CarrierRatesQueryV2.Data.Entities;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace CarrierRatesQueryV2.Api.Features.Carriers.Enable;

public sealed class Endpoint(AppDbContext appDbContext) : Endpoint<Request, Response>
{
    public override void Configure()
    {
        Patch("carriers/{id}/enable");
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