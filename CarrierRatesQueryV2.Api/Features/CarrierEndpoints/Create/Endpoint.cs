using CarrierRatesQueryV2.Data;
using CarrierRatesQueryV2.Data.Entities;
using FastEndpoints;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace CarrierRatesQueryV2.Api.Features.CarrierEndpoints.Create;

public sealed class Endpoint(AppDbContext appDbContext) : Endpoint<Request, Response>
{
    public override void Configure()
    {
        Post("carriers/{carrierId}/endpoints");
    }

    public override async Task HandleAsync(Request req, CancellationToken ct)
    {
        var carrierExists = await appDbContext.Carriers.AnyAsync(c => c.Id == req.CarrierId, ct);
        if (!carrierExists)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        var endpoint = new CarrierEndpoint
        {
            Id = Guid.NewGuid(),
            CarrierId = req.CarrierId,
            Operation = req.Operation,
            Endpoint = req.Endpoint
        };

        appDbContext.CarrierEndpoints.Add(endpoint);
        await appDbContext.SaveChangesAsync(ct);

        var response = new Response(endpoint.Id, endpoint.CarrierId, endpoint.Operation, endpoint.Endpoint);
        await Send.ResponseAsync(response, 201, ct);
    }
}

public sealed record Request(Guid CarrierId, string Operation, string Endpoint);

public sealed class Validator : Validator<Request>
{
    public Validator()
    {
        RuleFor(x => x.Operation).NotEmpty();
        RuleFor(x => x.Endpoint).NotEmpty();
    }
}

public sealed record Response(Guid Id, Guid CarrierId, string Operation, string Endpoint);

public class EndpointSummary : Summary<Endpoint>
{
    public EndpointSummary()
    {
        Summary = "Create a carrier endpoint";
        Description = "Creates a new endpoint configuration for a carrier, specifying the operation type and URL.";
        ExampleRequest = new Request(Guid.Empty, "rate", "https://api.carrier.com/rates");
        Response(201, "Endpoint created successfully", example: new Response(
            Guid.Empty,
            Guid.Empty,
            "rate",
            "https://api.carrier.com/rates"));
        Response(400, "Validation failed - operation or endpoint URL is empty");
        Response(404, "Carrier with the specified ID was not found");
    }
}