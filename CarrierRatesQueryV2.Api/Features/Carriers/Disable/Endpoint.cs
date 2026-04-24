using CarrierRatesQueryV2.Api.Infrastructure;
using CarrierRatesQueryV2.Data;
using CarrierRatesQueryV2.Data.Entities;
using FastEndpoints;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace CarrierRatesQueryV2.Api.Features.Carriers.Disable;

public sealed class Endpoint(
    AppDbContext appDbContext,
    IRequestRoleAccessor requestRoleAccessor) : Endpoint<Request, Response>
{
    public override void Configure()
    {
        Patch("carriers/{id}/disable");
    }

    public override async Task HandleAsync(Request req, CancellationToken ct)
    {
        var role = requestRoleAccessor.GetRequiredRole();
        if (role != RequestRole.Admin)
        {
            ThrowError("Only administrators can disable carriers directly. Use disable-request flow for regular users.", 403);
            return;
        }

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
    private readonly AppDbContext _db;

    public Validator(AppDbContext db)
    {
        _db = db;

        RuleFor(x => x.Reason)
            .NotEmpty()
            .WithMessage("Reason is required");

        RuleFor(x => x.Id)
            .MustAsync(BeLastEnabledCarrier)
            .WithMessage("Cannot disable the only enabled carrier.");
    }

    private async Task<bool> BeLastEnabledCarrier(Guid id, CancellationToken ct)
    {
        var carrier = await _db.Carriers.FirstOrDefaultAsync(c => c.Id == id, ct);
        if (carrier == null || !carrier.IsEnabled) return true; // allow if carrier doesn't exist or already disabled

        var enabledCount = await _db.Carriers.CountAsync(c => c.IsEnabled, ct);
        return enabledCount > 1;
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