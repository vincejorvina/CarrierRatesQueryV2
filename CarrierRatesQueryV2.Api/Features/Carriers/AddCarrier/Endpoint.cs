using CarrierRatesQueryV2.Data;
using CarrierRatesQueryV2.Data.Entities;
using FastEndpoints;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using GetCarrierByIdEndpoint = CarrierRatesQueryV2.Api.Features.Carriers.GetCarrierById.Endpoint;

namespace CarrierRatesQueryV2.Api.Features.Carriers.AddCarrier;

public sealed class Endpoint(AppDbContext appDbContext) : Endpoint<Request>
{
    public override void Configure()
    {
        Post("carriers");
        AllowAnonymous();
    }

    public override async Task HandleAsync(Request req, CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var carrier = new Carrier
        {
            Id = Guid.NewGuid(),
            Name = req.Name.Trim(),
            IsEnabled = req.IsEnabled,
            CreatedAtUtc = now
        };

        appDbContext.Carriers.Add(carrier);
        await appDbContext.SaveChangesAsync(ct);

        await Send.CreatedAtAsync<GetCarrierByIdEndpoint>(
            routeValues: new { id = carrier.Id },
            cancellation: ct);
    }
}

public sealed record Request(string Name, bool IsEnabled);

public sealed class Validator : Validator<Request>
{
    public Validator()
    {
        RuleFor(r => r.Name)
            .NotEmpty()
            .WithMessage("Name is required.")
            .MaximumLength(100)
            .WithMessage("Name must be less than 100 characters.")
            .MustAsync(BeUniqueName)
            .WithMessage("Name must be unique.");
    }

    private async Task<bool> BeUniqueName(string name, CancellationToken ct)
    {
        var appDbContext = Resolve<AppDbContext>();
        var exists = await appDbContext.Carriers.AnyAsync(r => r.Name == name, ct);
        return !exists;
    }
}