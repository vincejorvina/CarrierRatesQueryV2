using CarrierRatesQueryV2.Data;
using CarrierRatesQueryV2.Data.Entities;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace CarrierRatesQueryV2.Api.Features.Carriers.GetAll;

public sealed record Response(
    Guid Id,
    string Name,
    string Slug,
    bool IsEnabled,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc = null,
    List<CarrierEndpoint>? Endpoints = null
);

public sealed class Endpoint(AppDbContext appDbContext) : EndpointWithoutRequest<List<Response>>
{
    public override void Configure()
    {
        Get("carriers");
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var carriers = await appDbContext.Carriers
            .Include(c => c.Endpoints)
            .OrderBy(c => c.Name)
            .Select(c => new Response(c.Id, c.Name, c.Slug, c.IsEnabled, c.CreatedAtUtc, c.UpdatedAtUtc, c.Endpoints.OrderBy(e => e.Operation).ToList()))
            .ToListAsync(ct);

        await Send.OkAsync(carriers, ct);
    }
}