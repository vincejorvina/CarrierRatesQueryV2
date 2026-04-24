using CarrierRatesQueryV2.Data;
using FastEndpoints;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace CarrierRatesQueryV2.Api.Features.Carriers.Update;

public sealed class Endpoint(AppDbContext appDbContext) : Endpoint<Request>
{
    public override void Configure()
    {
        Put("carriers/{id}");
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

public sealed record Request(Guid Id, string? Name, bool? IsEnabled);

public sealed class Validator : Validator<Request>
{
    private readonly AppDbContext _db;

    public Validator(AppDbContext db)
    {
        _db = db;

        RuleFor(r => r.Name)
            .MaximumLength(100)
            .WithMessage("Name must be less than 100 characters.")
            .MustAsync(BeUniqueName)
            .WithMessage("Name must be unique.");
    }

    private async Task<bool> BeUniqueName(Request req, string name, CancellationToken ct)
    {
        var exists = await _db.Carriers.AnyAsync(r => r.Name == name && r.Id != req.Id, ct);
        return !exists;
    }
}