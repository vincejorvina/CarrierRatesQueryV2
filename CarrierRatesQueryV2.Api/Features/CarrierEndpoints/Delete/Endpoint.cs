using CarrierRatesQueryV2.Data;
using CarrierRatesQueryV2.Data.Entities;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace CarrierRatesQueryV2.Api.Features.CarrierEndpoints.Delete;

public sealed class Endpoint(AppDbContext appDbContext) : Endpoint<Request>
{
    public override void Configure()
    {
        Delete("carriers/{carrierId}/endpoints/{endpointId}");
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
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.CarrierId == req.CarrierId && e.Id == req.EndpointId, ct);

        if (endpoint == null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        appDbContext.CarrierEndpoints.Remove(endpoint);
        await appDbContext.SaveChangesAsync(ct);

        await Send.NoContentAsync(ct);
    }
}

public sealed record Request(Guid CarrierId, Guid EndpointId);

public class EndpointSummary : Summary<Endpoint>
{
    public EndpointSummary()
    {
        Summary = "Delete a carrier endpoint";
        Description = "Permanently deletes an endpoint configuration from a carrier.";
        Response(204, "Endpoint deleted successfully");
        Response(404, "Carrier or endpoint with the specified IDs was not found");
    }
}