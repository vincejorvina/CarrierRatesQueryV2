using CarrierRatesQueryV2.Data;
using CarrierRatesQueryV2.Data.Entities;
using FastEndpoints;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace CarrierRatesQueryV2.Api.Features.Carriers.Create;

public class EndpointSummary : Summary<Endpoint>
{
    public EndpointSummary()
    {
        Summary = "Create a new carrier";
        Description = "Creates a new carrier in the system. The carrier name must be unique and no longer than 100 characters.";
        ExampleRequest = new Request("FedEx Ground", true);
        Response(201, "Carrier created successfully", example: new Response(
            Guid.Empty,
            "FedEx Ground",
            "fedex-ground",
            true,
            DateTime.UtcNow));
        Response(400, "Validation failed - name is required, exceeds 100 characters, or is not unique");
    }
}

public sealed record Request(string Name, bool IsEnabled);

public sealed record Response(
    Guid Id,
    string Name,
    string Slug,
    bool IsEnabled,
    DateTime CreatedAtUtc
);

public sealed class Endpoint(AppDbContext appDbContext) : Endpoint<Request, Response>
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

        var response = new Response(carrier.Id, carrier.Name, carrier.Slug, carrier.IsEnabled, carrier.CreatedAtUtc);
        await Send.ResponseAsync(response, 201, ct);
    }
}

public sealed class Validator : Validator<Request>
{
    private readonly AppDbContext _db;
    public Validator(AppDbContext db)
    {
        _db = db;

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
        var exists = await _db.Carriers.AnyAsync(r => r.Name == name, ct);
        return !exists;
    }
}