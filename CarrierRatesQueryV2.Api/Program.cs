using CarrierRatesQueryV2.Api;
using CarrierRatesQueryV2.Api.Services;
using CarrierRatesQueryV2.Data;

public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.AddServiceDefaults();
        builder.Services.AddServices();

        var app = builder.Build();
        app.SetupFastEndpoints();

        app.SeedData();

        app.UseExceptionHandler();
        app.UseHttpsRedirection();

        await app.RunAsync();
    }

    public static WebApplication CreateHost(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.AddServiceDefaults();
        builder.Services.AddServices();

        var app = builder.Build();
        app.SetupFastEndpoints();

        app.SeedData();

        app.UseExceptionHandler();
        app.UseHttpsRedirection();

        return app;
    }
}