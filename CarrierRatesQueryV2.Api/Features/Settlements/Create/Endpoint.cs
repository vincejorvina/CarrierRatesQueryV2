using CarrierRatesQueryV2.Data;
using CarrierRatesQueryV2.Data.Entities;
using FastEndpoints;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace CarrierRatesQueryV2.Api.Features.Settlements.Create;

public class EndpointSummary : Summary<Endpoint>
{
    public EndpointSummary()
    {
        Summary = "Create a settlement";
        Description = "Creates a new settlement for a carrier.";
        ExampleRequest = new Request(Guid.Empty);
        Response(201, "Settlement created successfully");
        Response(404, "Carrier not found");
    }
}

public sealed record Request(Guid CarrierId);

public sealed record Response(Guid Id, Guid CarrierId, string Status, DateTime CreatedAtUtc);

public sealed class Endpoint(AppDbContext appDbContext) : Endpoint<Request, Response>
{
    public override void Configure()
    {
        Post("carriers/{carrierId}/settlements");
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

        var settlement = new CarrierFinancialSettlement
        {
            Id = Guid.NewGuid(),
            CarrierId = req.CarrierId,
            Status = CarrierFinancialSettlementStatus.Pending,
            CreatedAtUtc = DateTime.UtcNow
        };

        appDbContext.CarrierFinancialSettlements.Add(settlement);
        await appDbContext.SaveChangesAsync(ct);

        var response = new Response(settlement.Id, settlement.CarrierId, settlement.Status.ToString(), settlement.CreatedAtUtc);
        await Send.ResponseAsync(response, 201, ct);
    }
}

public sealed class Validator : Validator<Request>
{
    public Validator()
    {
        RuleFor(x => x.CarrierId)
            .NotEmpty()
            .WithMessage("CarrierId is required")
            .MustAsync(CarrierExists)
            .WithMessage("Carrier not found");
    }

    private async Task<bool> CarrierExists(Guid carrierId, CancellationToken ct)
    {
        var db = Resolve<AppDbContext>();
        return await db.Carriers.AnyAsync(c => c.Id == carrierId, ct);
    }
}