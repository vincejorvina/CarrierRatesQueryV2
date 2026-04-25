using CarrierRatesQueryV2.Data;
using CarrierRatesQueryV2.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace CarrierRatesQueryV2.Api.Services;

public interface ICarrierManagementService
{
    Task<(bool CanDisable, string? Reason)> ValidateCanDisableCarrierAsync(Guid carrierId, AppDbContext db);
    Task DisableCarrierAsync(Guid carrierId, string reason, string processedBy, AppDbContext db);
}

public class CarrierManagementService : ICarrierManagementService
{
    public async Task<(bool CanDisable, string? Reason)> ValidateCanDisableCarrierAsync(Guid carrierId, AppDbContext db)
    {
        var carrier = await db.Carriers.AsNoTracking().FirstOrDefaultAsync(c => c.Id == carrierId);
        if (carrier == null)
        {
            return (false, "Carrier not found");
        }

        if (!carrier.IsEnabled)
        {
            return (true, null);
        }

        var enabledCount = await db.Carriers.AsNoTracking().CountAsync(c => c.IsEnabled);
        var pendingRequestsForOtherCarriers = await db.DisableRequests
            .AsNoTracking()
            .Where(r => r.Status == DisableRequestStatus.Pending && r.CarrierId != carrierId)
            .CountAsync();

        var enabledAfterThisDisable = enabledCount - 1 - pendingRequestsForOtherCarriers;
        if (enabledAfterThisDisable < 1)
        {
            return (false, "Cannot disable carrier: would leave no enabled carriers after accounting for all pending disable requests.");
        }

        var hasPendingShipments = await db.Shipments.AsNoTracking().AnyAsync(s => s.CarrierId == carrierId && s.Status == ShipmentStatus.Pending);
        if (hasPendingShipments)
        {
            return (false, "Cannot disable carrier: has pending shipments.");
        }

        var hasPendingSettlements = await db.CarrierFinancialSettlements.AsNoTracking().AnyAsync(s => s.CarrierId == carrierId && s.Status == CarrierFinancialSettlementStatus.Pending);
        if (hasPendingSettlements)
        {
            return (false, "Cannot disable carrier: has pending settlements.");
        }

        return (true, null);
    }

    public async Task DisableCarrierAsync(Guid carrierId, string reason, string processedBy, AppDbContext db)
    {
        var carrier = await db.Carriers.AsTracking().FirstOrDefaultAsync(c => c.Id == carrierId);
        if (carrier == null) return;
        if (!carrier.IsEnabled) return;

        carrier.IsEnabled = false;
        carrier.UpdatedAtUtc = DateTime.UtcNow;

        db.CarrierDisableAudits.Add(new CarrierDisableAudit
        {
            Id = Guid.NewGuid(),
            CarrierId = carrier.Id,
            Reason = reason,
            ProcessedBy = processedBy,
            DisabledAtUtc = DateTime.UtcNow
        });

        await db.SaveChangesAsync();
    }
}