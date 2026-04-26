using CarrierRatesQueryV2.Api.Services;
using CarrierRatesQueryV2.Data;
using CarrierRatesQueryV2.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using Xunit;

namespace CarrierRatesQueryV2.Tests.Features.Services.Unit;

public class CarrierManagementServiceTests
{
    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"CarrierManagementService_{Guid.NewGuid()}")
            .Options;
        return new AppDbContext(options);
    }

    private static CarrierManagementService CreateService()
    {
        return new CarrierManagementService();
    }

    public class ValidateCanDisableCarrierAsync
    {
        [Fact]
        public async Task CarrierNotFound_ReturnsFalse()
        {
            var db = CreateDbContext();
            var service = CreateService();

            var result = await service.ValidateCanDisableCarrierAsync(Guid.NewGuid(), db);

            result.CanDisable.ShouldBeFalse();
            result.Reason.ShouldBe("Carrier not found");
        }

        [Fact]
        public async Task AlreadyDisabledCarrier_ReturnsTrue()
        {
            var db = CreateDbContext();
            var carrierId = Guid.NewGuid();
            db.Carriers.Add(new Carrier
            {
                Id = carrierId,
                Name = "Disabled Carrier",
                IsEnabled = false,
                CreatedAtUtc = DateTime.UtcNow
            });
            await db.SaveChangesAsync();

            var service = CreateService();

            var result = await service.ValidateCanDisableCarrierAsync(carrierId, db);

            result.CanDisable.ShouldBeTrue();
            result.Reason.ShouldBeNull();
        }

        [Fact]
        public async Task OnlyEnabledCarrier_NoPendingRequests_ReturnsFalse()
        {
            var db = CreateDbContext();
            var carrierId = Guid.NewGuid();
            db.Carriers.Add(new Carrier
            {
                Id = carrierId,
                Name = "Only Carrier",
                IsEnabled = true,
                CreatedAtUtc = DateTime.UtcNow
            });
            await db.SaveChangesAsync();

            var service = CreateService();

            var result = await service.ValidateCanDisableCarrierAsync(carrierId, db);

            result.CanDisable.ShouldBeFalse();
            result.Reason.ShouldContain("would leave no enabled carriers");
        }

        [Fact]
        public async Task TwoEnabledCarriers_NoPendingRequests_ReturnsTrue()
        {
            var db = CreateDbContext();
            var carrierId = Guid.NewGuid();
            db.Carriers.Add(new Carrier { Id = carrierId, Name = "Carrier A", IsEnabled = true, CreatedAtUtc = DateTime.UtcNow });
            db.Carriers.Add(new Carrier { Id = Guid.NewGuid(), Name = "Carrier B", IsEnabled = true, CreatedAtUtc = DateTime.UtcNow });
            await db.SaveChangesAsync();

            var service = CreateService();

            var result = await service.ValidateCanDisableCarrierAsync(carrierId, db);

            result.CanDisable.ShouldBeTrue();
            result.Reason.ShouldBeNull();
        }

[Fact]
        public async Task CarrierWithPendingShipment_ReturnsFalse()
        {
            var db = CreateDbContext();
            var carrierId = Guid.NewGuid();
            db.Carriers.Add(new Carrier { Id = carrierId, Name = "Carrier A", IsEnabled = true, CreatedAtUtc = DateTime.UtcNow });
            db.Carriers.Add(new Carrier { Id = Guid.NewGuid(), Name = "Carrier B", IsEnabled = true, CreatedAtUtc = DateTime.UtcNow });
            db.Shipments.Add(new Shipment
            {
                Id = Guid.NewGuid(),
                CarrierId = carrierId,
                Status = ShipmentStatus.Pending,
                CreatedAtUtc = DateTime.UtcNow
            });
            await db.SaveChangesAsync();

            var service = CreateService();

            var result = await service.ValidateCanDisableCarrierAsync(carrierId, db);

            result.CanDisable.ShouldBeFalse();
            result.Reason.ShouldContain("pending shipments");
        }

        [Fact]
        public async Task CarrierWithPendingSettlement_ReturnsFalse()
        {
            var db = CreateDbContext();
            var carrierId = Guid.NewGuid();
            db.Carriers.Add(new Carrier { Id = carrierId, Name = "Carrier A", IsEnabled = true, CreatedAtUtc = DateTime.UtcNow });
            db.Carriers.Add(new Carrier { Id = Guid.NewGuid(), Name = "Carrier B", IsEnabled = true, CreatedAtUtc = DateTime.UtcNow });
            db.CarrierFinancialSettlements.Add(new CarrierFinancialSettlement
            {
                Id = Guid.NewGuid(),
                CarrierId = carrierId,
                Status = CarrierFinancialSettlementStatus.Pending,
                CreatedAtUtc = DateTime.UtcNow
            });
            await db.SaveChangesAsync();

            var service = CreateService();

            var result = await service.ValidateCanDisableCarrierAsync(carrierId, db);

            result.CanDisable.ShouldBeFalse();
            result.Reason.ShouldContain("pending settlements");
        }

        [Fact]
        public async Task CarrierWithCompletedShipment_CanBeDisabled()
        {
            var db = CreateDbContext();
            var carrierId = Guid.NewGuid();
            db.Carriers.Add(new Carrier { Id = carrierId, Name = "Carrier A", IsEnabled = true, CreatedAtUtc = DateTime.UtcNow });
            db.Carriers.Add(new Carrier { Id = Guid.NewGuid(), Name = "Carrier B", IsEnabled = true, CreatedAtUtc = DateTime.UtcNow });
            db.Shipments.Add(new Shipment
            {
                Id = Guid.NewGuid(),
                CarrierId = carrierId,
                Status = ShipmentStatus.Completed,
                CreatedAtUtc = DateTime.UtcNow
            });
            await db.SaveChangesAsync();

            var service = CreateService();

            var result = await service.ValidateCanDisableCarrierAsync(carrierId, db);

            result.CanDisable.ShouldBeTrue();
            result.Reason.ShouldBeNull();
        }

        [Fact]
        public async Task CarrierWithSettledSettlement_CanBeDisabled()
        {
            var db = CreateDbContext();
            var carrierId = Guid.NewGuid();
            db.Carriers.Add(new Carrier { Id = carrierId, Name = "Carrier A", IsEnabled = true, CreatedAtUtc = DateTime.UtcNow });
            db.Carriers.Add(new Carrier { Id = Guid.NewGuid(), Name = "Carrier B", IsEnabled = true, CreatedAtUtc = DateTime.UtcNow });
            db.CarrierFinancialSettlements.Add(new CarrierFinancialSettlement
            {
                Id = Guid.NewGuid(),
                CarrierId = carrierId,
                Status = CarrierFinancialSettlementStatus.Settled,
                CreatedAtUtc = DateTime.UtcNow
            });
            await db.SaveChangesAsync();

            var service = CreateService();

            var result = await service.ValidateCanDisableCarrierAsync(carrierId, db);

            result.CanDisable.ShouldBeTrue();
            result.Reason.ShouldBeNull();
        }
    }

    public class DisableCarrierAsync
    {
        [Fact]
        public async Task CarrierNotFound_DoesNothing()
        {
            var db = CreateDbContext();
            var service = CreateService();

            await service.DisableCarrierAsync(Guid.NewGuid(), "reason", "admin", db);

            db.CarrierDisableAudits.Count().ShouldBe(0);
        }

        [Fact]
        public async Task AlreadyDisabledCarrier_DoesNothing()
        {
            var db = CreateDbContext();
            var carrierId = Guid.NewGuid();
            db.Carriers.Add(new Carrier
            {
                Id = carrierId,
                Name = "Disabled Carrier",
                IsEnabled = false,
                CreatedAtUtc = DateTime.UtcNow
            });
            await db.SaveChangesAsync();

            var service = CreateService();

            await service.DisableCarrierAsync(carrierId, "reason", "admin", db);

            var carrier = await db.Carriers.FirstOrDefaultAsync(c => c.Id == carrierId);
            carrier!.IsEnabled.ShouldBe(false);
            db.CarrierDisableAudits.Count().ShouldBe(0);
        }

        [Fact]
        public async Task EnabledCarrier_DisablesAndCreatesAudit()
        {
            var db = CreateDbContext();
            var carrierId = Guid.NewGuid();
            db.Carriers.Add(new Carrier
            {
                Id = carrierId,
                Name = "Test Carrier",
                IsEnabled = true,
                CreatedAtUtc = DateTime.UtcNow
            });
            await db.SaveChangesAsync();

            var service = CreateService();

            await service.DisableCarrierAsync(carrierId, "Test reason", "test-admin", db);

            var carrier = await db.Carriers.FirstOrDefaultAsync(c => c.Id == carrierId);
            carrier!.IsEnabled.ShouldBe(false);
            carrier.UpdatedAtUtc.ShouldNotBeNull();

            var audit = await db.CarrierDisableAudits.FirstOrDefaultAsync(a => a.CarrierId == carrierId);
            audit.ShouldNotBeNull();
            audit.Reason.ShouldBe("Test reason");
            audit.ProcessedBy.ShouldBe("test-admin");
            audit.DisabledAtUtc.ShouldBeGreaterThan(DateTime.MinValue);
        }
    }
}