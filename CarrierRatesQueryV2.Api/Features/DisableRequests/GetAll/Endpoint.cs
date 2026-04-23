using CarrierRatesQueryV2.Data;
using CarrierRatesQueryV2.Data.Entities;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace CarrierRatesQueryV2.Api.Features.DisableRequests.GetAll;

public sealed class Endpoint(AppDbContext appDbContext) : EndpointWithoutRequest<List<Response>>
{
    public override void Configure()
    {
        Get("disable-requests");
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var requests = await appDbContext.DisableRequests
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