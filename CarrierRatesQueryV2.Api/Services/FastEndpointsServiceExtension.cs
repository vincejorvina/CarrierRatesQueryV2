using FastEndpoints;
using FastEndpoints.Swagger;
using Microsoft.AspNetCore.Mvc;

namespace CarrierRatesQueryV2.Api.Services;

public static class FastEndpointsServiceExtension
{
    public static void SetupFastEndpoints(this IServiceCollection services)
    {
        services.AddFastEndpoints()
            .SwaggerDocument(o =>
            {
                o.EnableJWTBearerAuth = false;
                o.MaxEndpointVersion = 1;
                o.DocumentSettings = s =>
                {
                    s.Title = "CarrierRatesQuery API Service";
                    s.Description = "CarrierRatesQuery API Service";
                    s.Version = "v1";
                };
            });
    }

    public static void SetupFastEndpoints(this WebApplication app)
    {
        app.UseFastEndpoints(config =>
        {
            config.Versioning.Prefix = "v";
            config.Versioning.PrependToRoute = true;
            config.Versioning.DefaultVersion = 1;
            config.Endpoints.RoutePrefix = "api";

            config.Errors.StatusCode = StatusCodes.Status422UnprocessableEntity;

            config.Errors.ResponseBuilder = (failures, ctx, statusCode) =>
            {
                return new ValidationProblemDetails(
                    failures.GroupBy(f => f.PropertyName)
                    .ToDictionary(
                        keySelector: e => e.Key,
                        elementSelector: e => e.Select(m => m.ErrorMessage).ToArray()))
                {
                    Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
                    Title = "One or more validation errors occurred.",
                    Status = statusCode,
                    Instance = ctx.Request.Path,
                    Extensions = { { "traceId", ctx.TraceIdentifier } }
                };
            };
        }).UseSwaggerGen();
    }
}
