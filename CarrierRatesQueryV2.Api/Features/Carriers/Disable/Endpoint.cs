using CarrierRatesQueryV2.Api.Infrastructure;
using CarrierRatesQueryV2.Data;
using CarrierRatesQueryV2.Data.Entities;
using FastEndpoints;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace CarrierRatesQueryV2.Api.Features.Carriers.Disable;

public class EndpointSummary : Summary<Endpoint>
{
    public EndpointSummary()
    {
        Summary = "Disable a carrier";
        Description = "Immediately disables a carrier without requiring a disable request. Only administrators can use this endpoint. For regular users, use the disable request flow instead.";
        ExampleRequest = new Request(Guid.Empty, "Scheduled maintenance");
        Response(200, "Carrier has been disabled");
        Response(400, "Bad request - missing X-Role header, invalid X-Role value, or missing reason");
        Response(403, "Forbidden - only administrators can directly disable carriers");
        Response(404, "Carrier with the specified ID was not found");
        Response(409, "Conflict - cannot disable the only enabled carrier");
    }
}

public sealed record Request(Guid Id, string Reason);

public sealed record EndpointDto(Guid Id, Guid CarrierId, string Operation, string Endpoint);

public sealed record Response(
    Guid Id,
    string Name,
    string Slug,
    bool IsEnabled,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc = null,
    List<EndpointDto>? Endpoints = null
);

public sealed class Endpoint(
    AppDbContext appDbContext,
    IRequestRoleAccessor requestRoleAccessor) : Endpoint<Request, Response>
{
    public override void Configure()
    {
        Patch("carriers/{id}/disable");
        AllowAnonymous();
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
            .AsTracking()
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

        var response = new Response(
            Id: carrier.Id,
            Name: carrier.Name,
            Slug: carrier.Slug,
            IsEnabled: carrier.IsEnabled,
            CreatedAtUtc: carrier.CreatedAtUtc,
            UpdatedAtUtc: carrier.UpdatedAtUtc,
            Endpoints: carrier.Endpoints?.Select(e => new EndpointDto(e.Id, e.CarrierId, e.Operation, e.Endpoint)).OrderBy(e => e.Operation).ToList() ?? []
        );

        await Send.OkAsync(response, ct);
    }
}

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
        if (carrier == null || !carrier.IsEnabled) return true;

        var enabledCount = await _db.Carriers.CountAsync(c => c.IsEnabled, ct);
        return enabledCount > 1;
    }
}