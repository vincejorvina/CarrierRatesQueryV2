using CarrierRatesQueryV2.Api.Features.Rates.QueryAll;
using CarrierRatesQueryV2.Core.Contracts.Rates;
using CarrierRatesQueryV2.Core.Interfaces.Rates;
using CarrierRatesQueryV2.Data;
using CarrierRatesQueryV2.Data.Entities;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Shouldly;
using Xunit;

namespace CarrierRatesQueryV2.Tests.Features.Rates.QueryAll.Unit;

public class QueryAllRatesHandlerTests_MultiCarrier
{
    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"QueryAllRatesMulti_{Guid.NewGuid()}")
            .Options;
        return new AppDbContext(options);
    }

    private static Endpoint CreateEndpoint(AppDbContext db, ICarrierRateStrategyResolver resolver)
    {
        return Factory.Create<Endpoint>(db, resolver);
    }

    [Fact]
    public async Task HandleAsync_ThreeEnabledCarriers_ReturnsAllThreeRates()
    {
        var db = CreateDbContext();

        var fedexId = Guid.NewGuid();
        var dhlId = Guid.NewGuid();
        var upsId = Guid.NewGuid();

        db.Carriers.AddRange(
            new Carrier
            {
                Id = fedexId,
                Name = "FedEx",
                IsEnabled = true,
                CreatedAtUtc = DateTime.UtcNow,
                Endpoints = [new() { Id = Guid.NewGuid(), CarrierId = fedexId, Operation = "Rates", Endpoint = "http://fedex/api/rates" }]
            },
            new Carrier
            {
                Id = dhlId,
                Name = "DHL",
                IsEnabled = true,
                CreatedAtUtc = DateTime.UtcNow,
                Endpoints = [new() { Id = Guid.NewGuid(), CarrierId = dhlId, Operation = "Rates", Endpoint = "http://dhl/api/rates" }]
            },
            new Carrier
            {
                Id = upsId,
                Name = "UPS",
                IsEnabled = true,
                CreatedAtUtc = DateTime.UtcNow,
                Endpoints = [new() { Id = Guid.NewGuid(), CarrierId = upsId, Operation = "Rates", Endpoint = "http://ups/api/rates" }]
            }
        );
        await db.SaveChangesAsync();

        var fedexStrategy = Substitute.For<ICarrierRateStrategy>();
        fedexStrategy.CarrierSlug.Returns("fedex");
        fedexStrategy.TryGetRatesAsync(Arg.Any<CarrierContext>(), Arg.Any<RateQuery>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<ShippingRateQuote?>(new ShippingRateQuote("FedEx", [new RateOption("FedEx Ground", DateTime.UtcNow.AddDays(3), new Money(12.50m, "USD"))])));

        var dhlStrategy = Substitute.For<ICarrierRateStrategy>();
        dhlStrategy.CarrierSlug.Returns("dhl");
        dhlStrategy.TryGetRatesAsync(Arg.Any<CarrierContext>(), Arg.Any<RateQuery>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<ShippingRateQuote?>(new ShippingRateQuote("DHL", [new RateOption("DHL Express", DateTime.UtcNow.AddDays(2), new Money(22.00m, "USD"))])));

        var upsStrategy = Substitute.For<ICarrierRateStrategy>();
        upsStrategy.CarrierSlug.Returns("ups");
        upsStrategy.TryGetRatesAsync(Arg.Any<CarrierContext>(), Arg.Any<RateQuery>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<ShippingRateQuote?>(new ShippingRateQuote("UPS", [new RateOption("UPS Ground", DateTime.UtcNow.AddDays(4), new Money(10.00m, "USD"))])));

        var resolver = Substitute.For<ICarrierRateStrategyResolver>();
        resolver.TryResolve("fedex", out var fedex).Returns(x => { x[1] = fedexStrategy; return true; });
        resolver.TryResolve("dhl", out var dhl).Returns(x => { x[1] = dhlStrategy; return true; });
        resolver.TryResolve("ups", out var ups).Returns(x => { x[1] = upsStrategy; return true; });

        var endpoint = CreateEndpoint(db, resolver);

        var request = new Request(
            new LocationRequest("12345", "US"),
            new LocationRequest("67890", "US"),
            new PackageRequest(5, new PackageDimensionsRequest(10, 5, 5))
        );

        await endpoint.HandleAsync(request, CancellationToken.None);

        endpoint.Response.ShouldNotBeNull();
        endpoint.Response.Count.ShouldBe(3);
        endpoint.Response.ShouldContain(x => x.Carrier == "FedEx");
        endpoint.Response.ShouldContain(x => x.Carrier == "DHL");
        endpoint.Response.ShouldContain(x => x.Carrier == "UPS");
    }
}