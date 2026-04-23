using CarrierRatesQueryV2.Api.Infrastructure;
using CarrierRatesQueryV2.Data;
using CarrierRatesQueryV2.Data.Entities;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace CarrierRatesQueryV2.Api.Features.DisableRequests.Reject;

public sealed class Endpoint(
    AppDbContext appDbContext,
    IRequestRoleAccessor requestRoleAccessor) : Endpoint<Request, Response>
{
    public override void Configure()
    {
        Patch("disable-requests/{disableRequestId}/reject");
    }

    public override async Task HandleAsync(Request req, CancellationToken ct)
    {
        var role = requestRoleAccessor.GetRequiredRole();
        if (role != RequestRole.Admin)
        {
            ThrowError("Only administrators can reject disable requests.", 403);
            return;
        }

        var processedBy = requestRoleAccessor.GetRequestedBy();

        var disableRequest = await appDbContext.DisableRequests
            .FirstOrDefaultAsync(r => r.Id == req.DisableRequestId, ct);

        if (disableRequest == null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        if (disableRequest.Status != DisableRequestStatus.Pending)
        {
            ThrowError("Only pending disable requests can be rejected", 400);
            return;
        }

        disableRequest.Status = DisableRequestStatus.Rejected;
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