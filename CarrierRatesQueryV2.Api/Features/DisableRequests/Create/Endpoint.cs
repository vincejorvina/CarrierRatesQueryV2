using CarrierRatesQueryV2.Api.Infrastructure;
using CarrierRatesQueryV2.Api.Services;
using CarrierRatesQueryV2.Data;
using CarrierRatesQueryV2.Data.Entities;
using FastEndpoints;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace CarrierRatesQueryV2.Api.Features.DisableRequests.Create;

public class EndpointSummary : Summary<Endpoint>
{
    public EndpointSummary()
    {
        Summary = "Create a disable request";
        Description = "Creates a new request to disable a carrier. Validates that the carrier can be disabled (has no pending shipments, settlements, and would not leave the system with no enabled carriers).";
        ExampleRequest = new Request(Guid.Empty, "Carrier service degradation");
        Response(201, "Disable request created successfully", example: new Response(
            Guid.Empty,
            Guid.Empty,
            "admin",
            "Carrier service degradation",
            "Pending",
            DateTime.UtcNow,
            null,
            null));
        Response(400, "Bad request - carrier cannot be disabled (has pending shipments, settlements, or would leave no enabled carriers)");
        Response(404, "Carrier with the specified ID was not found");
    }
}

public sealed record Request(Guid CarrierId, string Reason);

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

public sealed class Endpoint(
    AppDbContext appDbContext,
    IRequestRoleAccessor requestRoleAccessor) : Endpoint<Request, Response>
{
    public override void Configure()
    {
        Post("carriers/{carrierId}/disable-requests");
        AllowAnonymous();
    }

    public override async Task HandleAsync(Request req, CancellationToken ct)
    {
        _ = requestRoleAccessor.GetRequiredRole();
        var requestedBy = requestRoleAccessor.GetRequestedBy();

        var carrierExists = await appDbContext.Carriers.AnyAsync(c => c.Id == req.CarrierId, ct);
        if (!carrierExists)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        var disableRequest = new DisableRequest
        {
            Id = Guid.NewGuid(),
            CarrierId = req.CarrierId,
            RequestedBy = requestedBy,
            Reason = req.Reason,
            Status = DisableRequestStatus.Pending,
            RequestedAtUtc = DateTime.UtcNow
        };

        appDbContext.DisableRequests.Add(disableRequest);
        await appDbContext.SaveChangesAsync(ct);

        var response = new Response(
            disableRequest.Id,
            disableRequest.CarrierId,
            disableRequest.RequestedBy,
            disableRequest.Reason,
            disableRequest.Status.ToString(),
            disableRequest.RequestedAtUtc,
            disableRequest.ProcessedBy,
            disableRequest.ProcessedAtUtc
        );
        await Send.ResponseAsync(response, 201, ct);
    }
}

public sealed class Validator : Validator<Request>
{
    private readonly AppDbContext _db;
    private readonly ICarrierManagementService _carrierManagementService;

    public Validator(AppDbContext db, ICarrierManagementService carrierManagementService)
    {
        _db = db;
        _carrierManagementService = carrierManagementService;

        RuleFor(x => x.Reason)
            .NotEmpty()
            .WithMessage("Reason is required");

        RuleFor(x => x.CarrierId)
            .MustAsync(BeAbleToDisableCarrier)
            .WithMessage("Cannot create disable request: carrier cannot be disabled.");
    }

    private async Task<bool> BeAbleToDisableCarrier(Guid carrierId, CancellationToken ct)
    {
        var (canDisable, _) = await _carrierManagementService.ValidateCanDisableCarrierAsync(carrierId, _db);
        return canDisable;
    }
}