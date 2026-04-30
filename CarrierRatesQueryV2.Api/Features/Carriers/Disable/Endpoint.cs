using CarrierRatesQueryV2.Api.Infrastructure;
using CarrierRatesQueryV2.Api.Services;
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
        Description = "Immediately disables a carrier without requiring a disable request. Only administrators can use this endpoint. Validates that disabling would not leave the system with no enabled carriers.";
        ExampleRequest = new Request(Guid.Empty, "Scheduled maintenance");
        Response(200, "Carrier has been disabled");
        Response(400, "Bad request - missing X-Role header, invalid X-Role value, or missing reason");
        Response(403, "Forbidden - only administrators can disable carriers");
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
    IRequestRoleAccessor requestRoleAccessor,
    ICarrierManagementService carrierManagementService) : Endpoint<Request, Response>
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
            var (canDisable, reason) = await carrierManagementService.ValidateCanDisableCarrierAsync(carrier.Id, appDbContext);
            if (!canDisable)
            {
                ThrowError($"Cannot disable carrier: {reason}", 409);
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
    private readonly ICarrierManagementService _carrierManagementService;

    public Validator(ICarrierManagementService carrierManagementService)
    {
        _carrierManagementService = carrierManagementService;

        RuleFor(x => x.Reason)
            .NotEmpty()
            .WithMessage("Reason is required");

        RuleFor(x => x.Id)
            .MustAsync(BeAbleToDisableCarrier)
            .WithMessage("Cannot disable carrier.");
    }

    private async Task<bool> BeAbleToDisableCarrier(Guid carrierId, CancellationToken ct)
    {
        var db = Resolve<AppDbContext>();
        var (canDisable, _) = await _carrierManagementService.ValidateCanDisableCarrierAsync(carrierId, db);
        return canDisable;
    }
}