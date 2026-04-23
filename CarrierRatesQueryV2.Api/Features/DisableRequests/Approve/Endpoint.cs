using CarrierRatesQueryV2.Api.Infrastructure;
using CarrierRatesQueryV2.Data;
using CarrierRatesQueryV2.Data.Entities;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace CarrierRatesQueryV2.Api.Features.DisableRequests.Approve;

public sealed class Endpoint(
    AppDbContext appDbContext,
    IRequestRoleAccessor requestRoleAccessor) : Endpoint<Request, Response>
{
    public override void Configure()
    {
        Patch("disable-requests/{disableRequestId}/approve");
    }

    public override async Task HandleAsync(Request req, CancellationToken ct)
    {
        var role = requestRoleAccessor.GetRequiredRole();
        if (role != RequestRole.Admin)
        {
            ThrowError("Only administrators can approve disable requests.", 403);
            return;
        }

        var processedBy = requestRoleAccessor.GetRequestedBy();

        var disableRequest = await appDbContext.DisableRequests
            .Include(r => r.Carrier)
            .ThenInclude(c => c!.Endpoints)
            .FirstOrDefaultAsync(r => r.Id == req.DisableRequestId, ct);

        if (disableRequest == null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        if (disableRequest.Status != DisableRequestStatus.Pending)
        {
            ThrowError("Only pending disable requests can be approved", 400);
            return;
        }

        var carrier = disableRequest.Carrier;
        if (carrier != null)
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
                Reason = disableRequest.Reason,
                DisabledAtUtc = DateTime.UtcNow
            });
        }

        disableRequest.Status = DisableRequestStatus.Approved;
        disableRequest.ProcessedBy = processedBy;
        disableRequest.ProcessedAtUtc = DateTime.UtcNow;

        await appDbContext.SaveChangesAsync(ct);

        Response = new Response(
            disableRequest.Id,
            disableRequest.CarrierId,
            disableRequest.RequestedBy,
            disableRequest.Reason,
            disableRequest.Status.ToString(),
            disableRequest.RequestedAtUtc,
            disableRequest.ProcessedBy,
            disableRequest.ProcessedAtUtc
        );

        await Send.OkAsync(ct);
    }
}

public sealed record Request(Guid DisableRequestId);

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