using CarrierRatesQueryV2.Api;
using CarrierRatesQueryV2.Data;
using CarrierRatesQueryV2.Data.Entities;
using CarrierRatesQueryV2.Data.Seeder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace CarrierRatesQueryV2.Tests.Infrastructure;

public class IntegrationTestBase : 
    IClassFixture<TestWebApplicationFactory>,
    IAsyncLifetime
{
    private static readonly SemaphoreSlim IntegrationLock = new(1, 1);

    protected readonly TestWebApplicationFactory Factory;
    protected HttpClient Client = null!;
    public List<Carrier> SeededCarriers => Factory.SeededCarriers;
    
    protected IntegrationTestBase(TestWebApplicationFactory factory)
    {
        Factory = factory;
    }
    
public virtual async Task InitializeAsync()
    {
        await IntegrationLock.WaitAsync();

        Client = Factory.CreateClient();
        await Factory.ResetDatabaseAsync();
    }

    protected async Task<Carrier> GetCarrierByNameAsync(string name)
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var carrier = await db.Carriers
            .Include(c => c.Endpoints)
            .FirstOrDefaultAsync(c => c.Name == name);

        return carrier ?? throw new InvalidOperationException($"Carrier '{name}' was not found in test data.");
    }

    protected async Task<T> QueryDbAsync<T>(Func<AppDbContext, Task<T>> query)
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        return await query(db);
    }

    protected async Task<Carrier> AddCarrierAsync(string name, bool isEnabled = true)
    {
        return await QueryDbAsync(async db =>
        {
            var carrier = new Carrier
            {
                Id = Guid.NewGuid(),
                Name = name,
                IsEnabled = isEnabled,
                CreatedAtUtc = DateTime.UtcNow
            };

            db.Carriers.Add(carrier);
            await db.SaveChangesAsync();

            return carrier;
        });
    }

    protected async Task<CarrierEndpoint> AddCarrierEndpointAsync(Guid carrierId)
    {
        return await QueryDbAsync(async db =>
        {
            var endpoint = new CarrierEndpoint
            {
                Id = Guid.NewGuid(),
                CarrierId = carrierId,
                Operation = "Rates",
                Endpoint = "https://example.test/rates"
            };

            db.CarrierEndpoints.Add(endpoint);
            await db.SaveChangesAsync();

            return endpoint;
        });
    }

    protected async Task<Shipment> AddShipmentAsync(Guid carrierId)
    {
        return await QueryDbAsync(async db =>
        {
            var shipment = new Shipment
            {
                Id = Guid.NewGuid(),
                CarrierId = carrierId,
                Status = ShipmentStatus.Pending,
                CreatedAtUtc = DateTime.UtcNow
            };

            db.Shipments.Add(shipment);
            await db.SaveChangesAsync();

            return shipment;
        });
    }

    protected async Task<CarrierFinancialSettlement> AddSettlementAsync(Guid carrierId)
    {
        return await QueryDbAsync(async db =>
        {
            var settlement = new CarrierFinancialSettlement
            {
                Id = Guid.NewGuid(),
                CarrierId = carrierId,
                Status = CarrierFinancialSettlementStatus.Pending,
                CreatedAtUtc = DateTime.UtcNow
            };

            db.CarrierFinancialSettlements.Add(settlement);
            await db.SaveChangesAsync();

            return settlement;
        });
    }

    protected async Task<DisableRequest> AddDisableRequestAsync(Guid carrierId)
    {
        return await QueryDbAsync(async db =>
        {
            var request = new DisableRequest
            {
                Id = Guid.NewGuid(),
                CarrierId = carrierId,
                RequestedBy = "integration-user",
                Reason = "Temporary maintenance",
                Status = DisableRequestStatus.Pending,
                RequestedAtUtc = DateTime.UtcNow
            };

            db.DisableRequests.Add(request);
            await db.SaveChangesAsync();

            return request;
        });
    }

    protected Task<DisableRequest> GetPendingDisableRequestAsync()
    {
        return QueryDbAsync(db => db.DisableRequests
            .Where(r => r.Status == DisableRequestStatus.Pending)
            .OrderBy(r => r.RequestedAtUtc)
            .FirstAsync());
    }

    protected static object CreateRateRequest(object routeValues)
    {
        var values = routeValues.GetType().GetProperties().ToDictionary(p => p.Name, p => p.GetValue(routeValues));
        values["origin"] = new { postalCode = "12345", countryCode = "US" };
        values["destination"] = new { postalCode = "67890", countryCode = "US" };
        values["package"] = new { weight = 5m, dimensions = new { length = 10m, width = 5m, height = 5m } };

        return values;
    }

    protected static async Task<JsonElement> ReadJsonAsync(HttpResponseMessage response)
    {
        using var document = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
        return document.RootElement.Clone();
    }

    protected static Guid GetGuid(JsonElement element, string propertyName) => element.GetProperty(propertyName).GetGuid();

    protected static string GetString(JsonElement element, string propertyName) => element.GetProperty(propertyName).GetString() ?? string.Empty;

    protected static bool GetBoolean(JsonElement element, string propertyName) => element.GetProperty(propertyName).GetBoolean();

    protected static async Task ShouldHaveStatusAsync(HttpResponseMessage response, HttpStatusCode expectedStatusCode)
    {
        var body = await response.Content.ReadAsStringAsync();
        response.StatusCode.ShouldBe(expectedStatusCode, body);
    }

    protected static HttpRequestMessage CreateRoleRequest(HttpMethod method, string requestUri, string role, string? requestedBy = null)
    {
        var request = new HttpRequestMessage(method, requestUri);
        request.Headers.Add("X-Role", role);

        if (!string.IsNullOrWhiteSpace(requestedBy))
        {
            request.Headers.Add("X-Requested-By", requestedBy);
        }

        return request;
    }
    
    public virtual Task DisposeAsync()
    {
        Client.Dispose();
        IntegrationLock.Release();
        return Task.CompletedTask;
    }
}
