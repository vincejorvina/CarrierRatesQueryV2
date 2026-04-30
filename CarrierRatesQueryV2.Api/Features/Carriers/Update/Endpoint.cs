using CarrierRatesQueryV2.Data;
using FastEndpoints;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace CarrierRatesQueryV2.Api.Features.Carriers.Update;

public class EndpointSummary : Summary<Endpoint>
{
    public EndpointSummary()
    {
        Summary = "Update a carrier";
        Description = "Updates an existing carrier. Name can be modified here; enabling is allowed, but disabling must use the disable endpoint or disable-request workflow.";
        ExampleRequest = new Request(Guid.Empty, "Updated Carrier Name", null);
        Response(204, "Carrier updated successfully");
        Response(400, "Validation failed - name exceeds 100 characters or is not unique");
        Response(404, "Carrier with the specified ID was not found");
    }
}

public sealed record Request(Guid Id, string? Name, bool? IsEnabled);

public sealed class Endpoint(AppDbContext appDbContext) : Endpoint<Request>
{
    public override void Configure()
    {
        Put("carriers/{id}");
        AllowAnonymous();
    }

    public override async Task HandleAsync(Request req, CancellationToken ct)
    {
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

        var now = DateTime.UtcNow;

        if (req.Name != null)
        {
            carrier.Name = req.Name.Trim();
        }

        if (req.IsEnabled.HasValue)
        {
            if (req.IsEnabled.Value == false && carrier.IsEnabled)
            {
                ThrowError("Use the carrier disable endpoint or disable-request workflow to disable carriers.", 400);
                return;
            }

            carrier.IsEnabled = req.IsEnabled.Value;
        }

        carrier.UpdatedAtUtc = now;

        appDbContext.Carriers.Update(carrier);
        await appDbContext.SaveChangesAsync(ct);

        var updatedCarrier = await appDbContext.Carriers
            .Include(c => c.Endpoints)
            .FirstOrDefaultAsync(c => c.Id == carrier.Id, ct);

        if (updatedCarrier == null)
        {
            Response = null!;
            await Send.NotFoundAsync(ct);
            return;
        }

        await Send.NoContentAsync(ct);
    }
}

public sealed class Validator : Validator<Request>
{
    public Validator()
    {
        RuleFor(r => r.Name)
            .MaximumLength(100)
            .WithMessage("Name must be less than 100 characters.")
            .MustAsync(BeUniqueName)
            .WithMessage("Name must be unique.");
    }

    private async Task<bool> BeUniqueName(Request req, string name, CancellationToken ct)
    {
        var db = Resolve<AppDbContext>();
        var exists = await db.Carriers.AnyAsync(r => r.Name == name && r.Id != req.Id, ct);
        return !exists;
    }
}
