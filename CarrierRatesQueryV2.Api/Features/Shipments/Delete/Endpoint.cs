using CarrierRatesQueryV2.Data;
using CarrierRatesQueryV2.Data.Entities;
using FastEndpoints;

namespace CarrierRatesQueryV2.Api.Features.Shipments.Delete;

public class EndpointSummary : Summary<Endpoint>
{
    public EndpointSummary()
    {
        Summary = "Delete a shipment by ID";
        Description = "Removes a shipment from the system.";
        Response(204, "Shipment deleted");
        Response(404, "Shipment not found");
    }
}

public sealed record Request(Guid ShipmentId);

public sealed class Endpoint(AppDbContext appDbContext) : Endpoint<Request>
{
    public override void Configure()
    {
        Delete("shipments/{shipmentId}");
        AllowAnonymous();
    }

    public override async Task HandleAsync(Request req, CancellationToken ct)
    {
        var shipment = await appDbContext.Shipments.FindAsync([req.ShipmentId], ct);
        if (shipment == null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        appDbContext.Shipments.Remove(shipment);
        await appDbContext.SaveChangesAsync(ct);

        await Send.NoContentAsync(ct);
    }
}