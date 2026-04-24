using CarrierRatesQueryV2.Api.Features.Carriers.Create;
using CarrierRatesQueryV2.Data;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using System.Xml.Linq;
using Xunit;

namespace CarrierRatesQueryV2.Tests.Features.Carriers.Create.Unit;

public class CreateCarrierHandlerTests
{
    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"CreateCarrierHandler_{Guid.NewGuid()}")
            .Options;
        return new AppDbContext(options);
    }

    private static Endpoint CreateEndpoint(AppDbContext db)
    {
        var endpoint = Factory.Create<Endpoint>(db);
        return endpoint;
    }

    [Fact]
    public async Task HandleAsync_ValidRequest_ShouldCreateCarrier()
    {
        // Arrange
        var db = CreateDbContext();
        var endpoint = CreateEndpoint(db);

        var request = new Request("Test Carrier", true);

        // Act
        await endpoint.HandleAsync(request, CancellationToken.None);

        // Assert
        endpoint.Response.ShouldNotBeNull();
        endpoint.Response.Name.ShouldBe(request.Name);
        endpoint.Response.Slug.ShouldBe("testcarrier");
        endpoint.Response.IsEnabled.ShouldBe(request.IsEnabled);

        var carrier = await db.Carriers.FirstOrDefaultAsync(c => c.Name == request.Name);
        carrier.ShouldNotBeNull();
        carrier.Name.ShouldBe(request.Name);
        carrier.Slug.ShouldBe("testcarrier");
        carrier.IsEnabled.ShouldBe(request.IsEnabled);
    }

    [Fact]
    public async Task HandleAsync_ShortName_ShouldCreateCarrier()
    {
        // Arrange
        var db = CreateDbContext();
        var endpoint = CreateEndpoint(db);

        var request = new Request("A", true);

        // Act
        await endpoint.HandleAsync(request, CancellationToken.None);

        // Assert
        endpoint.Response.ShouldNotBeNull();
        endpoint.Response.Name.ShouldBe("A");
        endpoint.Response.Slug.ShouldBe("a");

        var carrier = await db.Carriers.FirstOrDefaultAsync(c => c.Name == request.Name);
        carrier.ShouldNotBeNull();
        carrier.Slug.ShouldBe("a");
    }

    [Fact]
    public async Task HandleAsync_NameWithUppercase_ShouldGenerateLowercaseSlug()
    {
        // Arrange
        var db = CreateDbContext();
        var endpoint = CreateEndpoint(db);

        var request = new Request("TEST", true);

        // Act
        await endpoint.HandleAsync(request, CancellationToken.None);

        // Assert
        endpoint.Response.ShouldNotBeNull();
        endpoint.Response.Slug.ShouldBe("test");

        var carrier = await db.Carriers.FirstOrDefaultAsync(c => c.Name == request.Name);
        carrier.ShouldNotBeNull();
        carrier.Slug.ShouldBe("test");
    }

    [Fact]
    public async Task HandleAsync_NameWithNumbers_ShouldPreserveNumbersInSlug()
    {
        // Arrange
        var db = CreateDbContext();
        var endpoint = CreateEndpoint(db);

        var request = new Request("Carrier123", true);

        // Act
        await endpoint.HandleAsync(request, CancellationToken.None);

        // Assert
        endpoint.Response.ShouldNotBeNull();
        endpoint.Response.Slug.ShouldBe("carrier123");

        var carrier = await db.Carriers.FirstOrDefaultAsync(c => c.Name == request.Name);
        carrier.ShouldNotBeNull();
        carrier.Slug.ShouldBe("carrier123");
    }
}
