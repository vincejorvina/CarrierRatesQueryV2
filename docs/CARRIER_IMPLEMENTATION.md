# Adding a New Carrier

Follow this guide to add support for a new carrier (e.g., USPS, LaserShip, etc.).

For architectural background, see [ARCHITECTURE.md](./ARCHITECTURE.md).

---

## Architecture Overview

Each carrier is implemented across 5 layers:

```
┌────────────────────────────────────────────────┐
│ API Layer (CarrierRatesQueryV2.Api)            │
│  ┌──────────────────────────────────────────┐  │
│  │ *RefitClient   ← HTTP calls via Refit    │  │
│  └──────────────────────────────────────────┘  │
└────────────────────────────────────────────────┘
                       ↓
┌────────────────────────────────────────────────┐
│ Core Layer (CarrierRatesQueryV2.Core)          │
│  ┌──────────────────────────────────────────┐  │
│  │ *Contracts   ← Request/response DTOs     │  │
│  │ IMock*Client ← Client interface          │  │
│  │ *Adapter     ← Normalizes response       │  │
│  │ *Strategy    ← Orchestrates + caching    │  │
│  └──────────────────────────────────────────┘  │
└────────────────────────────────────────────────┘
```

---

## Step 1: Define Contracts

Create request/response DTOs in `Core/Rates/Clients/`.

**File:** `Core/Rates/Clients/Mock{Carrier}Contracts.cs`

```csharp
namespace CarrierRatesQueryV2.Core.Rates.Clients;

public sealed record Mock{Carrier}RateRequest(Mock{Carrier}Package Package);

public sealed record Mock{Carrier}Package(decimal Weight, Mock{Carrier}Dimensions Dimensions);

public sealed record Mock{Carrier}Dimensions(decimal Length, decimal Width, decimal Height);

public sealed record Mock{Carrier}RateResponse(
    string Carrier,
    IReadOnlyList<Mock{Carrier}ServiceOption> ServiceOptions);

public sealed record Mock{Carrier}ServiceOption(
    string ServiceName,
    string EstimatedDelivery,
    decimal Rate);
```

---

## Step 2: Create Client Interface

Create interface in `Core/Interfaces/Rates/Clients/`.

**File:** `Core/Interfaces/Rates/Clients/IMock{Carrier}RatesClient.cs`

```csharp
using CarrierRatesQueryV2.Core.Rates.Clients;

namespace CarrierRatesQueryV2.Core.Interfaces.Rates.Clients;

public interface IMock{Carrier}RatesClient
{
    Task<Mock{Carrier}RateResponse> GetRatesAsync(
        string endpoint,
        Mock{Carrier}RateRequest request,
        CancellationToken cancellationToken);
}
```

---

## Step 3: Implement Refit Client

Create in `Api/Infrastructure/Rates/Clients/`.

**File:** `Api/Infrastructure/Rates/Clients/{Carrier}RefitClient.cs`

```csharp
using CarrierRatesQueryV2.Api.Services;
using CarrierRatesQueryV2.Core.Interfaces.Rates.Clients;
using CarrierRatesQueryV2.Core.Rates.Clients;
using Microsoft.Extensions.Logging;
using Refit;

namespace CarrierRatesQueryV2.Api.Infrastructure.Rates.Clients;

public interface I{Carrier}RatesRefitApi
{
    [Post("")]
    Task<Mock{Carrier}RateResponse> GetRatesAsync(
        [Body] Mock{Carrier}RateRequest request,
        CancellationToken cancellationToken);
}

public sealed class {Carrier}RefitClient(
    IHttpClientFactory httpClientFactory,
    ILogger<{Carrier}RefitClient> logger,
    ICarrierFailureTracker failureTracker) : IMock{Carrier}RatesClient
{
    private readonly string _clientName = nameof({Carrier}RefitClient);

    public async Task<Mock{Carrier}RateResponse> GetRatesAsync(
        string endpoint,
        Mock{Carrier}RateRequest request,
        CancellationToken cancellationToken)
    {
        if (!Uri.TryCreate(endpoint, UriKind.Absolute, out var endpointUri))
        {
            throw new ArgumentException("Endpoint must be a valid absolute URL.", nameof(endpoint));
        }

        if (failureTracker.IsCarrierFailing("{slug}"))
        {
            logger.LogWarning("{Carrier} carrier is currently failing, skipping request");
            throw new InvalidOperationException("{Carrier} carrier is temporarily unavailable");
        }

        try
        {
            var client = httpClientFactory.CreateClient(_clientName);
            client.BaseAddress = endpointUri;

            var api = RestService.For<I{Carrier}RatesRefitApi>(client);
            var response = await api.GetRatesAsync(request, cancellationToken);

            failureTracker.RecordSuccess("{slug}");
            logger.LogInformation("{Carrier} rates retrieved successfully");

            return response;
        }
        catch (Exception ex) when (ex is not InvalidOperationException)
        {
            failureTracker.RecordFailure("{slug}");
            logger.LogError(ex, "Failed to retrieve {Carrier} rates after retries");
            throw;
        }
    }
}
```

**Replace placeholders:**
- `{Carrier}` → FedEx, UPS, DHL, etc.
- `{slug}` → fedex, ups, dhl (lowercase for failure tracker)

---

## Step 4: Create Adapter

Create in `Core/Rates/Adapters/`.

**File:** `Core/Rates/Adapters/{Carrier}RateAdapter.cs`

```csharp
using CarrierRatesQueryV2.Core.Contracts.Rates;
using CarrierRatesQueryV2.Core.Interfaces.Rates;
using CarrierRatesQueryV2.Core.Rates.Clients;

namespace CarrierRatesQueryV2.Core.Rates.Adapters;

public sealed class {Carrier}RateAdapter : ICarrierRateAdapter<Mock{Carrier}RateResponse>
{
    public ShippingRateQuote Adapt(Mock{Carrier}RateResponse source)
    {
        var options = source.ServiceOptions.Select(x => new RateOption(
            ServiceName: x.ServiceName,
            EstimatedDelivery: DateTime.Parse(
                x.EstimatedDelivery,
                System.Globalization.CultureInfo.InvariantCulture),
            Price: new Money(x.Rate, "USD")))
            .ToList();

        return new ShippingRateQuote(source.Carrier, options);
    }
}
```

**Note:** The adapter normalizes carrier-specific response to our standard `ShippingRateQuote`.

---

## Step 5: Implement Strategy

Create in `Core/Rates/Strategies/`.

**File:** `Core/Rates/Strategies/{Carrier}RateStrategy.cs`

```csharp
using CarrierRatesQueryV2.Core.Contracts.Rates;
using CarrierRatesQueryV2.Core.Interfaces.Rates;
using CarrierRatesQueryV2.Core.Interfaces.Rates.Clients;
using CarrierRatesQueryV2.Core.Rates;
using CarrierRatesQueryV2.Core.Rates.Clients;

namespace CarrierRatesQueryV2.Core.Rates.Strategies;

public sealed class {Carrier}RateStrategy(
    IMock{Carrier}RatesClient mock{Carrier}RatesClient,
    ICarrierRateAdapter<Mock{Carrier}RateResponse> {carrier}RateAdapter,
    IRateCache rateCache) : ICarrierRateStrategy
{
    public string CarrierSlug => "{slug}";

    public async Task<ShippingRateQuote?> TryGetRatesAsync(
        CarrierContext carrier,
        RateQuery query,
        CancellationToken cancellationToken)
    {
        var cached = await rateCache.GetAsync(carrier, query, cancellationToken);
        if (cached is not null)
        {
            return cached;
        }

        var endpoint = carrier.Endpoints
            .FirstOrDefault(x => x.Operation.Equals("Rates", StringComparison.OrdinalIgnoreCase));

        if (endpoint is null)
        {
            return null;
        }

        var {carrier}Request = new Mock{Carrier}RateRequest(
            new Mock{Carrier}Package(
                query.Package.Weight,
                new Mock{Carrier}Dimensions(
                    query.Package.Dimensions.Length,
                    query.Package.Dimensions.Width,
                    query.Package.Dimensions.Height)));

        var response = await mock{Carrier}RatesClient.GetRatesAsync(
            endpoint.Endpoint,
            {carrier}Request,
            cancellationToken);

        var quote = {carrier}RateAdapter.Adapt(response);
        await rateCache.SetAsync(carrier, query, quote, cancellationToken);

        return quote;
    }
}
```

**Replace:**
- `{slug}` → fedex, ups, dhl (lowercase)
- `{Carrier}` → FedEx, Ups, Dhl (capitalized for class names, camelCase for fields)
- `{carrier}` → camelCase version

---

## Step 6: Register in DI

Register in **both** dependency injection files.

### Core/DependencyInjection.cs

```csharp
services.AddScoped<ICarrierRateStrategy, {Carrier}RateStrategy>();
services.AddScoped<ICarrierRateAdapter<Mock{Carrier}RateResponse>, {Carrier}RateAdapter>();
```

### Api/DependencyInjection.cs

```csharp
// HTTP client pipeline
services.AddCarrierHttpClient(nameof({Carrier}RefitClient));

// Client implementation
services.AddScoped<IMock{Carrier}RatesClient, {Carrier}RefitClient>();
```

---

## Step 7: Seed Data (Optional)

Add carrier to `Data/Seeder/DataSeeder.cs`:

```csharp
new Carrier { Name = "{Carrier}", Slug = "{slug}", IsEnabled = true, ... }
```

Or use the Create Carrier endpoint via API.

---

## Testing

### Unit Tests to Write

| Test File                                                    | Purpose                     |
|--------------------------------------------------------------|-----------------------------|
| `Tests/Features/Services/Unit/{Carrier}RateStrategyTests.cs` | Test strategy orchestration |
| `Tests/Features/Services/Unit/{Carrier}RateAdapterTests.cs`  | Test response normalization |

### Example: Strategy Test

```csharp
[Fact]
public async Task TryGetRatesAsync_ValidRequest_ReturnsQuote()
{
    // Arrange
    var mockClient = new Mock<IMock{Carrier}RatesClient>();
    mockClient
        .Setup(x => x.GetRatesAsync(It.IsAny<string>(), It.IsAny<Request>(), It.IsAny<CancellationToken>())
        .ReturnsAsync(new Mock{Carrier}RateResponse(...));

    var adapter = new {Carrier}RateAdapter();
    var cache = new MemoryRateCache();
    var strategy = new {Carrier}RateStrategy(mockClient.Object, adapter, cache);

    // Act
    var result = await strategy.TryGetRatesAsync(carrier, query, CancellationToken.None);

    // Assert
    result.ShouldNotBeNull();
    result.Carrier.ShouldBe("{slug}");
}
```

### Example: Adapter Test

```csharp
[Fact]
public void Adapt_ValidResponse_ReturnsNormalizedQuote()
{
    // Arrange
    var adapter = new {Carrier}RateAdapter();
    var response = new Mock{Carrier}RateResponse(...);

    // Act
    var quote = adapter.Adapt(response);

    // Assert
    quote.Carrier.ShouldBe("{slug}");
    quote.RateOptions.ShouldNotBeEmpty();
}
```

---

## Common Pitfalls

### 1. DateFormat Mismatch
The mock API returns dates as strings. Ensure the adapter uses `CultureInfo.InvariantCulture`:

```csharp
DateTime.Parse(x.EstimatedDelivery, System.Globalization.CultureInfo.InvariantCulture)
```

### 2. Missing "Rates" Endpoint
The strategy looks for an endpoint with `Operation = "Rates"`. Make sure the carrier has this endpoint configured.

### 3. Failure Tracker Key Mismatch
The failure tracker uses lowercase slug. Ensure consistency:

```csharp
// In RefitClient
failureTracker.IsCarrierFailing("{slug}");  // must match carrier.Slug
```

### 4. Missing DI Registration
If the carrier returns 404, check that both Core and Api DI registrations are complete.

---

## Quick Reference: File Summary

| Layer     | File Path                                                    | Purpose                     |
|-----------|--------------------------------------------------------------|-----------------------------|
| Contracts | `Core/Rates/Clients/Mock{carrier}Contracts.cs`               | DTOs                        |
| Interface | `Core/Interfaces/Rates/Clients/IMock{carrier}RatesClient.cs` | Abstraction                 |
| Client    | `Api/Infrastructure/Rates/Clients/{carrier}RefitClient.cs`   | HTTP                        |
| Adapter   | `Core/Rates/Adapters/{carrier}RateAdapter.cs`                | Normalize                   |
| Strategy  | `Core/Rates/Strategies/{carrier}RateStrategy.cs`             | Orchestrate                 |
| DI (Core) | `Core/DependencyInjection.cs`                                | Register strategy + adapter |
| DI (Api)  | `Api/DependencyInjection.cs`                                 | Register HTTP client + impl |