using CarrierRatesQueryV2.Data;
using CarrierRatesQueryV2.Data.Entities;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace CarrierRatesQueryV2.Api.Features.Settlements.Delete;

public class EndpointSummary : Summary<Endpoint>
{
    public EndpointSummary()
    {
        Summary = "Delete a settlement by ID";
        Description = "Removes a settlement from the system.";
        Response(204, "Settlement deleted");
        Response(404, "Settlement not found");
    }
}

public sealed record Request(Guid SettlementId);

public sealed class Endpoint(AppDbContext appDbContext) : Endpoint<Request>
{
    public override void Configure()
    {
        Delete("settlements/{settlementId}");
        AllowAnonymous();
    }

    public override async Task HandleAsync(Request req, CancellationToken ct)
    {
        var settlement = await appDbContext.CarrierFinancialSettlements
            .AsTracking()
            .FirstOrDefaultAsync(s => s.Id == req.SettlementId, ct);
        if (settlement == null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        appDbContext.CarrierFinancialSettlements.Remove(settlement);
        await appDbContext.SaveChangesAsync(ct);

        await Send.NoContentAsync(ct);
    }
}
