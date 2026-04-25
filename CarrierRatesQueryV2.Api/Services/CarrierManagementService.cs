using CarrierRatesQueryV2.Data;
using CarrierRatesQueryV2.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace CarrierRatesQueryV2.Api.Services;

public interface ICarrierManagementService
{
    Task<bool> CanDisableCarrierAsync(Guid carrierId, AppDbContext db);
    Task DisableCarrierAsync(Guid carrierId, string reason, string processedBy, AppDbContext db);
}

public class CarrierManagementService : ICarrierManagementService
{
    public async Task<bool> CanDisableCarrierAsync(Guid carrierId, AppDbContext db)
    {
        var carrier = await db.Carriers.AsNoTracking().FirstOrDefaultAsync(c => c.Id == carrierId);
        if (carrier == null) return false;
        if (!carrier.IsEnabled) return true;

        var enabledCount = await db.Carriers.AsNoTracking().CountAsync(c => c.IsEnabled);
        if (enabledCount <= 1) return false;

        var hasPendingShipments = await db.Shipments.AsNoTracking().AnyAsync(s => s.CarrierId == carrierId && s.Status == ShipmentStatus.Pending);
        if (hasPendingShipments) return false;

        var hasPendingSettlements = await db.CarrierFinancialSettlements.AsNoTracking().AnyAsync(s => s.CarrierId == carrierId && s.Status == CarrierFinancialSettlementStatus.Pending);
        if (hasPendingSettlements) return false;

        return true;
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
            DisabledAtUtc = DateTime.UtcNow
        });

        await db.SaveChangesAsync();
    }
}