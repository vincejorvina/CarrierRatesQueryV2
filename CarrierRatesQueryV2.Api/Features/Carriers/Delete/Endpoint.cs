using CarrierRatesQueryV2.Data;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace CarrierRatesQueryV2.Api.Features.Carriers.Delete;

public class EndpointSummary : Summary<Endpoint>
{
    public EndpointSummary()
    {
        Summary = "Delete a carrier";
        Description = "Permanently deletes a carrier and all its associated endpoints from the system.";
        Response(204, "Carrier deleted successfully");
        Response(404, "Carrier with the specified ID was not found");
    }
}

public sealed record Request(Guid Id);

public sealed class Endpoint(AppDbContext appDbContext) : Endpoint<Request>
{
    public override void Configure()
    {
        Delete("carriers/{id}");
        AllowAnonymous();
    }

    public override async Task HandleAsync(Request req, CancellationToken ct)
    {
        var carrier = await appDbContext.Carriers
            .AsTracking()
            .Include(c => c.Endpoints)
            .FirstOrDefaultAsync(c => c.Id == req.Id, ct);

        if (carrier == null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        appDbContext.Carriers.Remove(carrier);
        await appDbContext.SaveChangesAsync(ct);

        await Send.NoContentAsync(ct);
    }
}