using CarrierRatesQueryV2.Data;
using CarrierRatesQueryV2.Data.Entities;
using FastEndpoints;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace CarrierRatesQueryV2.Api.Features.Carriers.Disable;

public sealed class Endpoint(AppDbContext appDbContext) : Endpoint<Request, Response>
{
    public override void Configure()
    {
        Patch("carriers/{id}/disable");
    }

    public override async Task HandleAsync(Request req, CancellationToken ct)
    {
        var carrier = await appDbContext.Carriers
            .Include(c => c.Endpoints)
            .FirstOrDefaultAsync(c => c.Id == req.Id, ct);

        if (carrier == null)
        {
            Response = null!;
            await Send.NotFoundAsync(ct);
            return;
        }

        if (carrier.IsEnabled)
        {
            var enabledCount = await appDbContext.Carriers
                .CountAsync(c => c.IsEnabled, ct);

            if (enabledCount <= 1)
            {
                ThrowError("Cannot disable the only enabled carrier", 409);
                return;
            }

            var hasPendingShipments = await appDbContext.Shipments
                .AnyAsync(s => s.CarrierId == carrier.Id && s.Status == ShipmentStatus.Pending, ct);

            if (hasPendingShipments)
            {
                ThrowError("Cannot disable carrier with pending shipments", 409);
                return;
            }

            var hasPendingSettlements = await appDbContext.CarrierFinancialSettlements
                .AnyAsync(s => s.CarrierId == carrier.Id && s.Status == CarrierFinancialSettlementStatus.Pending, ct);

            if (hasPendingSettlements)
            {
                ThrowError("Cannot disable carrier with pending settlements", 409);
                return;
            }

            carrier.IsEnabled = false;
            carrier.UpdatedAtUtc = DateTime.UtcNow;

            appDbContext.CarrierDisableAudits.Add(new CarrierDisableAudit
            {
                Id = Guid.NewGuid(),
                CarrierId = carrier.Id,
                Reason = req.Reason,
                DisabledAtUtc = DateTime.UtcNow
            });

            await appDbContext.SaveChangesAsync(ct);
        }

        Response = new Response(
            Id: carrier.Id,
            Name: carrier.Name,
            Slug: carrier.Slug,
            IsEnabled: carrier.IsEnabled,
            CreatedAtUtc: carrier.CreatedAtUtc,
            UpdatedAtUtc: carrier.UpdatedAtUtc,
            Endpoints: carrier.Endpoints?.OrderBy(e => e.Operation).ToList() ?? []
        );

        await Send.OkAsync(ct);
    }
}

public sealed record Request(Guid Id, string Reason);

public sealed class Validator : Validator<Request>
{
    public Validator()
    {
        RuleFor(x => x.Reason)
            .NotEmpty()
            .WithMessage("Reason is required");
    }
}

public sealed record Response(
    Guid Id,
    string Name,
    string Slug,
    bool IsEnabled,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc = null,
    List<CarrierRatesQueryV2.Data.Entities.CarrierEndpoint>? Endpoints = null
);