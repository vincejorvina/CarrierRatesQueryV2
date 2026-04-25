using CarrierRatesQueryV2.Data;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace CarrierRatesQueryV2.Api.Features.Carriers.GetAll;

public class EndpointSummary : Summary<Endpoint>
{
    public EndpointSummary()
    {
        Summary = "Get all carriers";
        Description = "Retrieves a list of all carriers in the system.";
        Response(200, "Returns a list of all carriers");
    }
}

public sealed record Response(
    Guid Id,
    string Name,
    string Slug,
    bool IsEnabled,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc = null
);

public sealed class Endpoint(AppDbContext appDbContext) : EndpointWithoutRequest<List<Response>>
{
    public override void Configure()
    {
        Get("carriers");
        AllowAnonymous();
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var carriers = await appDbContext.Carriers
            .AsNoTracking()
            .ToListAsync(ct);

        var response = carriers
            .OrderBy(c => c.Name)
            .Select(c => new Response(c.Id, c.Name, c.Slug, c.IsEnabled, c.CreatedAtUtc, c.UpdatedAtUtc))
            .ToList();

        await Send.OkAsync(response, ct);
    }
}