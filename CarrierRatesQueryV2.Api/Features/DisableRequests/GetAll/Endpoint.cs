using CarrierRatesQueryV2.Api.Infrastructure;
using CarrierRatesQueryV2.Data;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace CarrierRatesQueryV2.Api.Features.DisableRequests.GetAll;

public class EndpointSummary : Summary<Endpoint>
{
    public EndpointSummary()
    {
        Summary = "Get all disable requests";
        Description = "Retrieves all disable requests in the system, ordered by most recently requested.";
        Response(200, "Returns a list of all disable requests");
        Response(400, "Bad request - missing or invalid X-Role header");
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

public sealed class Endpoint(
    AppDbContext appDbContext,
    IRequestRoleAccessor requestRoleAccessor) : EndpointWithoutRequest<List<Response>>
{
    public override void Configure()
    {
        Get("disable-requests");
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        _ = requestRoleAccessor.GetRequiredRole();

        var requests = await appDbContext.DisableRequests
            .OrderByDescending(r => r.RequestedAtUtc)
            .ToListAsync(ct);

        Response = requests.Select(r => new Response(
            r.Id,
            r.CarrierId,
            r.RequestedBy,
            r.Reason,
            r.Status.ToString(),
            r.RequestedAtUtc,
            r.ProcessedBy,
            r.ProcessedAtUtc
        )).ToList();

        await Send.OkAsync(ct);
    }
}