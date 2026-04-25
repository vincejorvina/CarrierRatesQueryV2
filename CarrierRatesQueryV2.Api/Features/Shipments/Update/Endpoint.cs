using CarrierRatesQueryV2.Data;
using CarrierRatesQueryV2.Data.Entities;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace CarrierRatesQueryV2.Api.Features.Shipments.Update;

public class EndpointSummary : Summary<Endpoint>
{
    public EndpointSummary()
    {
        Summary = "Update a shipment";
        Description = "Updates a shipment status (e.g., mark as Completed).";
        ExampleRequest = new Request(Guid.Empty, "Completed");
        Response(200, "Shipment updated");
        Response(400, "Invalid status");
        Response(404, "Shipment not found");
    }
}

public sealed record Request(Guid Id, string Status);

public sealed record Response(Guid Id, Guid CarrierId, string Status, DateTime CreatedAtUtc);

public sealed class Endpoint(AppDbContext appDbContext) : Endpoint<Request, Response>
{
    public override void Configure()
    {
        Patch("shipments/{id}");
        AllowAnonymous();
    }

    public override async Task HandleAsync(Request req, CancellationToken ct)
    {
        var shipment = await appDbContext.Shipments.FindAsync([req.Id], ct);
        if (shipment == null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        if (!Enum.TryParse<ShipmentStatus>(req.Status, ignoreCase: true, out var newStatus))
        {
            ThrowError("Invalid status. Valid values: Pending, Completed");
            return;
        }

        shipment.Status = newStatus;
        await appDbContext.SaveChangesAsync(ct);

        var response = new Response(shipment.Id, shipment.CarrierId, shipment.Status.ToString(), shipment.CreatedAtUtc);
        await Send.OkAsync(response, ct);
    }
}