using CarrierRatesQueryV2.Api;
using CarrierRatesQueryV2.Api.Services;
using CarrierRatesQueryV2.Data;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.AddServiceDefaults();
        builder.Services.AddServices();

        var app = builder.Build();
        app.SetupFastEndpoints();

        app.SeedData();

        app.UseExceptionHandler();
        app.UseHttpsRedirection();

        app.Run();
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