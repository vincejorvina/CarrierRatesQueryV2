using CarrierRatesQueryV2.Data;
using CarrierRatesQueryV2.Data.Entities;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace CarrierRatesQueryV2.Api.Features.DisableRequests.GetByCarrier;

public sealed class Endpoint(AppDbContext appDbContext) : Endpoint<Request, List<Response>>
{
    public override void Configure()
    {
        Get("carriers/{carrierId}/disable-requests");
    }

    public override async Task HandleAsync(Request req, CancellationToken ct)
    {
        var carrierExists = await appDbContext.Carriers.AnyAsync(c => c.Id == req.CarrierId, ct);
        if (!carrierExists)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        var requests = await appDbContext.DisableRequests
            .Where(r => r.CarrierId == req.CarrierId)
            .OrderByDescending(r => r.RequestedAtUtc)
            .ToListAsync(ct);

        Response = requests.Select(r => new Response(
            r.Id,
            r.CarrierId,
            r.RequestedBy,
            r.Reason,
            r.Status.ToString(),
            r.RequestedAtUtc,
            r.ProcessedBy,
            r.ProcessedAtUtc
        )).ToList();

        await Send.OkAsync(ct);
    }
}

public sealed record Request(Guid CarrierId);

public sealed record Response(
    Guid Id,
    Guid CarrierId,
    string RequestedBy,
    string Reason,
    string Status,
    DateTime RequestedAtUtc,
    string? ProcessedBy,
    DateTime? ProcessedAtUtc
);