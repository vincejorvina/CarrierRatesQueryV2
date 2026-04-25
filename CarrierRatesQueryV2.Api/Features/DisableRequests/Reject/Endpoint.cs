using CarrierRatesQueryV2.Api.Infrastructure;
using CarrierRatesQueryV2.Data;
using CarrierRatesQueryV2.Data.Entities;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace CarrierRatesQueryV2.Api.Features.DisableRequests.Reject;

public class EndpointSummary : Summary<Endpoint>
{
    public EndpointSummary()
    {
        Summary = "Reject a disable request";
        Description = "Rejects a pending disable request. Only administrators can reject requests.";
        Response(200, "Disable request rejected", example: new Response(
            Guid.Empty,
            Guid.Empty,
            "admin",
            "Carrier service degradation",
            "Rejected",
            DateTime.UtcNow,
            "admin",
            DateTime.UtcNow));
        Response(400, "Bad request - missing or invalid X-Role header, or request is not in pending status");
        Response(403, "Forbidden - only administrators can reject disable requests");
        Response(404, "Disable request with the specified ID was not found");
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

public sealed class Endpoint(
    AppDbContext appDbContext,
    IRequestRoleAccessor requestRoleAccessor) : Endpoint<Request, Response>
{
    public override void Configure()
    {
        Patch("disable-requests/{disableRequestId}/reject");
        AllowAnonymous();
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

        await Send.OkAsync(response, ct);
    }
}