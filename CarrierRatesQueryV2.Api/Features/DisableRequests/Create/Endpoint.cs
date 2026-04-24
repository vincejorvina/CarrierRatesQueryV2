using CarrierRatesQueryV2.Api.Infrastructure;
using CarrierRatesQueryV2.Data;
using CarrierRatesQueryV2.Data.Entities;
using FastEndpoints;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace CarrierRatesQueryV2.Api.Features.DisableRequests.Create;

public sealed class Endpoint(
    AppDbContext appDbContext,
    IRequestRoleAccessor requestRoleAccessor) : Endpoint<Request, Response>
{
    public override void Configure()
    {
        Post("carriers/{carrierId}/disable-requests");
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

public sealed record Request(Guid CarrierId, string Reason);

public sealed class Validator : Validator<Request>
{
    public Validator()
    {
        RuleFor(x => x.Reason).NotEmpty();
    }
}

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