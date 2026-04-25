using CarrierRatesQueryV2.Data;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace CarrierRatesQueryV2.Api.Features.DisableRequests.GetById;

public sealed class Endpoint(AppDbContext appDbContext) : Endpoint<Request, Response>
{
    public override void Configure()
    {
        Get("disable-requests/{disableRequestId}");
    }

    public override async Task HandleAsync(Request req, CancellationToken ct)
    {
        var disableRequest = await appDbContext.DisableRequests
            .FirstOrDefaultAsync(r => r.Id == req.DisableRequestId, ct);

        if (disableRequest == null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

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

public class EndpointSummary : Summary<Endpoint>
{
    public EndpointSummary()
    {
        Summary = "Get a disable request by ID";
        Description = "Retrieves a single disable request by its unique identifier.";
        Response(200, "Returns the disable request", example: new Response(
            Guid.Empty,
            Guid.Empty,
            "admin",
            "Carrier service degradation",
            "Pending",
            DateTime.UtcNow,
            null,
            null));
        Response(404, "Disable request with the specified ID was not found");
    }
}