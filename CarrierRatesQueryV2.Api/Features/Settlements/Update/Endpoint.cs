using CarrierRatesQueryV2.Data;
using CarrierRatesQueryV2.Data.Entities;
using FastEndpoints;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace CarrierRatesQueryV2.Api.Features.Settlements.Update;

public class EndpointSummary : Summary<Endpoint>
{
    public EndpointSummary()
    {
        Summary = "Update a settlement";
        Description = "Updates a settlement status (e.g., mark as Settled).";
        ExampleRequest = new Request(Guid.Empty, "Settled");
        Response(200, "Settlement updated");
        Response(400, "Invalid status");
        Response(404, "Settlement not found");
    }
}

public sealed record Request(Guid Id, string Status);

public sealed record Response(Guid Id, Guid CarrierId, string Status, DateTime CreatedAtUtc);

public sealed class Endpoint(AppDbContext appDbContext) : Endpoint<Request, Response>
{
    public override void Configure()
    {
        Patch("settlements/{id}");
        AllowAnonymous();
    }

    public override async Task HandleAsync(Request req, CancellationToken ct)
    {
        var settlement = await appDbContext.CarrierFinancialSettlements
            .AsTracking()
            .FirstOrDefaultAsync(s => s.Id == req.Id, ct);
        if (settlement == null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        if (!Enum.TryParse<CarrierFinancialSettlementStatus>(req.Status, ignoreCase: true, out var newStatus))
        {
            ThrowError("Invalid status. Valid values: Pending, Settled");
            return;
        }

        settlement.Status = newStatus;
        await appDbContext.SaveChangesAsync(ct);

        var response = new Response(settlement.Id, settlement.CarrierId, settlement.Status.ToString(), settlement.CreatedAtUtc);
        await Send.OkAsync(response, ct);
    }
}

public sealed class Validator : Validator<Request>
{
    private static readonly string[] ValidStatuses = ["Pending", "Settled"];

    public Validator()
    {
        RuleFor(x => x.Id)
            .NotEmpty()
            .WithMessage("Id is required");

        RuleFor(x => x.Status)
            .NotEmpty()
            .WithMessage("Status is required")
            .Must(status => ValidStatuses.Contains(status, StringComparer.OrdinalIgnoreCase))
            .WithMessage($"Invalid status. Valid values: {string.Join(", ", ValidStatuses)}");
    }
}