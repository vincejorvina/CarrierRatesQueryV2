using CarrierRatesQueryV2.Data;
using CarrierRatesQueryV2.Data.Entities;
using FastEndpoints;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace CarrierRatesQueryV2.Api.Features.Shipments.Create;

public class EndpointSummary : Summary<Endpoint>
{
    public EndpointSummary()
    {
        Summary = "Create a shipment";
        Description = "Creates a new shipment for a carrier.";
        ExampleRequest = new Request(Guid.Empty);
        Response(201, "Shipment created successfully");
        Response(404, "Carrier not found");
    }
}

public sealed record Request(Guid CarrierId);

public sealed record Response(Guid Id, Guid CarrierId, string Status, DateTime CreatedAtUtc);

public sealed class Endpoint(AppDbContext appDbContext) : Endpoint<Request, Response>
{
    public override void Configure()
    {
        Post("carriers/{carrierId}/shipments");
        AllowAnonymous();
    }

    public override async Task HandleAsync(Request req, CancellationToken ct)
    {
        var carrierExists = await appDbContext.Carriers.AnyAsync(c => c.Id == req.CarrierId, ct);
        if (!carrierExists)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        var shipment = new Shipment
        {
            Id = Guid.NewGuid(),
            CarrierId = req.CarrierId,
            Status = ShipmentStatus.Pending,
            CreatedAtUtc = DateTime.UtcNow
        };

        appDbContext.Shipments.Add(shipment);
        await appDbContext.SaveChangesAsync(ct);

        var response = new Response(shipment.Id, shipment.CarrierId, shipment.Status.ToString(), shipment.CreatedAtUtc);
        await Send.ResponseAsync(response, 201, ct);
    }
}

public sealed class Validator : Validator<Request>
{
    private readonly AppDbContext _db;

    public Validator(AppDbContext db)
    {
        _db = db;

        RuleFor(x => x.CarrierId)
            .NotEmpty()
            .WithMessage("CarrierId is required")
            .MustAsync(CarrierExists)
            .WithMessage("Carrier not found");
    }

    private async Task<bool> CarrierExists(Guid carrierId, CancellationToken ct)
    {
        return await _db.Carriers.AnyAsync(c => c.Id == carrierId, ct);
    }
}