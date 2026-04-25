# AGENTS.md

## Purpose

This repository uses **FastEndpoints** with a vertical slice architecture.  
This document defines how coding agents should understand, extend, and test the system.

The goal is to ensure:
- consistent architecture decisions
- correct use of FastEndpoints
- reliable and maintainable tests

---

## High-Level Architecture

- **FastEndpoints** handles HTTP layer
- **Core project** contains business logic
- **Data project** handles persistence (EF Core)
- **Features** are organized by vertical slices

Each feature is self-contained:
- Endpoint
- Request/Response DTOs
- Validator
- Logic orchestration

---

## Design Principles

- Prefer **vertical slice architecture**
- Keep **business logic in Core**, not in endpoints
- Treat validation as a **boundary concern**
- Use **dependency injection** for all services
- Avoid cross-feature coupling

---

## Request Flow

```
HTTP Request
   ↓
Model Binding
   ↓
Validation (FluentValidation)
   ↓
Endpoint (HandleAsync)
   ↓
Core Services (Business Logic)
   ↓
Response
```

---

## EF Core Change Tracking

AppDbContext is configured with `AsNoTracking()` by default. Always use `AsTracking()` explicitly for write operations to ensure EF Core tracks changes for save.

**For write operations (Create, Update, Delete):**
```csharp
var entity = await db.Carriers
    .AsTracking()
    .FirstOrDefaultAsync(c => c.Id == id);
```

Read-only operations use AsNoTracking() by default and don't need to be specified.

---

## Testing Strategy (IMPORTANT)

Testing is intentionally split into **three distinct layers**.

Agents must NOT mix these responsibilities.

---

## 1. Endpoint Unit Tests

### Purpose
Test endpoint behavior and orchestration logic only.

### Characteristics
- Uses `Factory.Create<TEndpoint>()`
- Calls `HandleAsync()` directly
- Does NOT execute pipeline

### Rules
- Assume input is **valid**
- Do NOT test validation here
- Do NOT expect middleware behavior

### Example

```csharp
var ep = Factory.Create<MyEndpoint>(db);

await ep.HandleAsync(validRequest, ct);

ep.Response.ShouldNotBeNull();
```

### Naming Convention

Use the pattern: `MethodUnderTest_State_ExpectedBehavior`

Example: `HandleAsync_ValidRequest_ShouldCreateCarrier`

### Structure

Use the Arrange-Act-Assert pattern:

```csharp
// Arrange
var db = CreateDbContext();
var endpoint = Factory.Create<Endpoint>(db);
var request = new Request("Test Carrier", true);

// Act
await endpoint.HandleAsync(request, CancellationToken.None);

// Assert
endpoint.Response.ShouldNotBeNull();
var carrier = await db.Carriers.FirstOrDefaultAsync(c => c.Name == request.Name);
carrier.ShouldNotBeNull();
```

### What to Test
- Response values
- DB persistence (verify data was saved)
- DB side effects
- Response mapping
- Business flow orchestration

---

## 2. Validator Tests

### Purpose
Ensure request validation rules are correct.

### Characteristics
- Tests validator in isolation
- Uses the validator's test helper from FluentValidation

### Example

```csharp
var db = CreateDbContext();
var validator = new Validator(db);
var request = new Request("Test Carrier", true);

var result = await validator.TestValidateAsync(request);

result.ShouldNotHaveAnyValidationErrors();
result.ShouldHaveValidationErrorFor(x => x.Name)
    .WithErrorMessage("Name is required.");
```

### What to Test
- Required fields
- Format rules
- Unique constraints
- Edge cases

---

## 3. Integration Tests (Full Pipeline)

### Purpose
Verify full system behavior including:
- validation
- routing
- endpoint execution

### Characteristics
- Uses `WebApplicationFactory`
- Uses real HTTP calls
- **Sequential execution** via `[Collection("IntegrationTests")]`
- **Database reset** between tests via `EnsureDeletedAsync` + `EnsureCreatedAsync` + reseed

### Infrastructure

Located in `CarrierRatesQueryV2.Tests/Infrastructure/`:

| File | Purpose |
|------|---------|
| `TestWebApplicationFactory.cs` | WebApplicationFactory with in-memory DB |
| `IntegrationTestBase.cs` | Base class with `Client` and DB reset |
| `CarrierFailureSimulationFixture.cs` | Placeholder for HTTP mocking |

### Creating Integration Tests

```csharp
[Collection("IntegrationTests")]
public class MyFeatureTests : IntegrationTestBase
{
    public MyFeatureTests(TestWebApplicationFactory f) : base(f) { }
    
    [Fact]
    public async Task Endpoint_WithValidRequest_ShouldReturn200()
    {
        var response = await Client.PostAsJsonAsync("/api/v1/endpoint", request);
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }
}
```

**Important:** 
- Route prefix is `/api/v1/` (FastEndpoints versioning)
- Use `[Collection("IntegrationTests")]` to run sequentially
- Database is reset between tests via `InitializeAsync`

### Example

```csharp
var client = factory.CreateClient();

var response = await client.PostAsJsonAsync("/api/resource", new {
    name = ""
});

response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
```

---

## Database Isolation

Each integration test MUST use a unique database instance.

### Pattern

```csharp
factory.WithWebHostBuilder(builder =>
{
    builder.ConfigureServices(services =>
    {
        services.RemoveAll<DbContextOptions<AppDbContext>>();

        services.AddDbContext<AppDbContext>(opt =>
            opt.UseInMemoryDatabase(Guid.NewGuid().ToString()));
    });
});
```

---

## Anti-Patterns (DO NOT DO)

### ❌ Testing validation in endpoint unit tests

```csharp
await ep.HandleAsync(invalidRequest);
```

### ❌ Manually invoking validators inside endpoint tests

### ❌ Sharing database state across tests

### ❌ Recreating FastEndpoints pipeline manually

---

## Design Guidelines

- Endpoints should assume valid input
- Validation should live in validators
- Critical constraints should be enforced at:
  - database level, or
  - domain/service level

---

## Test Distribution

- Unit tests → many, fast
- Validator tests → many, cheap
- Integration tests → few, high-value

---

## Test Location Convention

Tests are organized by type:

**Core Service Tests** - Tests for business logic services (e.g., CarrierFailureTracker, RateQueryService):

```
CarrierRatesQueryV2.Tests/Features/Services/Unit/
```

**Endpoint Tests** - Tests for FastEndpoints (e.g., CreateCarrier, QueryRates):

```
CarrierRatesQueryV2.Tests/Features/<Category>/<Feature>/
```

**Validator Tests** - Tests for FluentValidation validators:

```
CarrierRatesQueryV2.Tests/Features/<Category>/<Feature>/
```

**Integration Tests** - Full pipeline tests:

```
CarrierRatesQueryV2.Tests/Integration/
```

Use Shouldly assertions, not xUnit's Assert. Example:

```csharp
result.ShouldBeTrue();
result.ShouldNotBeNull();
```

---

## Core Services

### CarrierFailureTracker

Tracks carrier failures to prevent repeated calls to failing carriers.

**Interface:** `ICarrierFailureTracker`

**Implementation:** `CarrierFailureTracker` (in `CarrierRatesQueryV2.Api/Services/`)

**Location:** `CarrierRatesQueryV2.Api/Services/CarrierFailureTracker.cs`

**Methods:**
- `IsCarrierFailing(string carrierSlug)` - Returns true if carrier has failed within the last 30 seconds
- `RecordFailure(string carrierSlug)` - Records a failure with 30-second cache duration
- `RecordSuccess(string carrierSlug)` - Clears failure state on successful call

**Usage:** Injected into Refit clients (FedExRefitClient, DhlRefitClient, UpsRefitClient) to handle carrier failures gracefully.

**Tests:** `CarrierRatesQueryV2.Tests/Features/Services/Unit/CarrierFailureTrackerTests.cs`

---

## Resilience Configuration

HTTP clients are configured with retry and circuit breaker patterns in `DependencyInjection.cs`.

### Retry Configuration
- Max attempts: 3
- Delay: 2 seconds with jitter

### Circuit Breaker Configuration
- Sampling duration: 30 seconds
- Failure ratio: 50%
- Minimum throughput: 5 requests
- Break duration: 30 seconds

Configuration is in `AddCarrierHttpClient()` method.

---

## Carrier Disable Flow

### Business Rules

The system validates carrier disable operations at **both** create and approve to prevent race conditions.

**Validation runs on:**
1. **Create** - disable request creation validates business rules early
2. **Approve** - approval re-validates to handle race conditions

**Key Logic:** When checking if a carrier can be disabled, the system accounts for **ALL pending disable requests**:

```csharp
var enabledCount = await db.Carriers.CountAsync(c => c.IsEnabled);
var pendingRequestsForOtherCarriers = await db.DisableRequests
    .Where(r => r.Status == DisableRequestStatus.Pending && r.CarrierId != carrierId)
    .CountAsync();

var enabledAfterThisDisable = enabledCount - 1 - pendingRequestsForOtherCarriers;
```

This prevents the race condition where multiple disable requests could leave zero enabled carriers when approved sequentially.

**Service:** `ICarrierManagementService.ValidateCanDisableCarrierAsync()`

---

## Intentional Design Decisions

The following were identified during code review but are **NOT bugs** - they are intentional design choices.

### Cache Key Does Not Include Origin/Destination

**Location:** `CarrierRatesQueryV2.Core/Rates/MemoryRateCache.cs`

The cache key only includes package weight/dimensions and carrier configuration. Origin and destination are NOT included because:

- The mock services (FedEx, DHL, UPS) only accept package weight/dimensions in their requests
- Origin/destination are part of the API contract but are not passed to carrier APIs
- Therefore, they do not affect the rate returned and should not be part of the cache key

If switching to real carrier APIs that use origin/destination for rating, the cache key should be updated accordingly.

### Enable Endpoint Has No Validation

**Location:** `CarrierRatesQueryV2.Api/Features/Carriers/Enable/Endpoint.cs`

The enable endpoint simply flips `IsEnabled = true` without validation. This is intentional:

- Intended as an admin "switch" for quick enable/disable
- The update endpoint already handles carrier modifications
- Disabling is the operation with business rules (which are enforced)
- Re-enabling a carrier has no business constraints

---

## Final Notes

- Do not try to simulate the full pipeline in unit tests
- If HTTP behavior is involved, use integration tests
- Prefer clarity over clever abstractions
- If unable to resolve an issue after a few attempts, or if the infrastructure or architecture has to change, stop and consult the user before going ahead with a fix
