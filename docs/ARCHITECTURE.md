# Architecture & Technical Details

For quickstart, see [README.md](../README.md).

---

## Solution Structure

```
CarrierRatesQueryV2/
├── CarrierRatesQueryV2.Api/       # Main API
├── CarrierRatesQueryV2.Core/      # Business logic
├── CarrierRatesQueryV2.Data/        # EF Core entities
├── CarrierRatesQueryV2.Tests/     # Tests
├── CarrierRatesQueryV2.AppHost/   # Aspire orchestration
├── CarrierRatesQueryV2.MockFedEx/ # Mock carrier APIs
├── CarrierRatesQueryV2.MockUps/
├── CarrierRatesQueryV2.MockDhl/
└── CarrierRatesQueryV2.ServiceDefaults/
```

---

## Design Patterns

### Strategy Pattern
`ICarrierRateStrategy` with implementations for FedEx, UPS, DHL. The resolver dynamically selects which strategy to use based on carrier slug.

### Adapter Pattern
`ICarrierRateAdapter<T>` normalizes carrier-specific responses into `ShippingRateQuote`.

### Open/Closed Principle
New carriers can be added by implementing `ICarrierRateStrategy` and `ICarrierRateAdapter<T>` - no existing code changes needed.

---

## Request Flow

```
HTTP Request
   ↓
Model Binding (FastEndpoints)
   ↓
Validation (FluentValidation)
   ↓
Endpoint.HandleAsync()
   ↓
Core Services (Strategy → Adapter → Cache)
   ↓
Response
```

---

## Testing Strategy

Three distinct layers - see [AGENTS.md](../AGENTS.md) for full details.

### Unit Tests
Use `Factory.Create<TEndpoint>()` - tests endpoint behavior in isolation.

### Validator Tests
Use validator's `TestValidateAsync()` - tests validation rules.

### Integration Tests
Use `WebApplicationFactory` - full HTTP pipeline.

### Test Location Convention

```
Tests/
├── Features/
│   ├── <Feature>/
│   │   ├── Unit/           # Endpoint tests
│   │   └── Validator/      # Validator tests
│   └── Services/
│       └── Unit/           # Service tests
└── Integration/           # Full pipeline tests
```

---

## Key Components

### CarrierDisableAudit
Tracks carrier disable actions with:
- `CarrierId` - which carrier was disabled
- `Reason` - why (maintenance, contract termination, etc.)
- `ProcessedBy` - who performed the action
- `DisabledAtUtc` - when

### MemoryRateCache
In-memory cache for rate queries.
- TTL: 2 minutes
- Key: carrier config + package weight/dimensions

### CarrierFailureTracker
In-memory failure tracking (30-second cache) to prevent repeated calls to failing carriers.

---

## Business Rules

### Carrier Disable Flow

Validation runs on **both** create and approve:

1. **Create** - disable request creation validates early
2. **Approve** - approval re-validates to handle race conditions

**Race Condition Prevention:**
```csharp
var enabledCount = await db.Carriers.CountAsync(c => c.IsEnabled);
var pendingRequestsForOtherCarriers = await db.DisableRequests
    .Where(r => r.Status == DisableRequestStatus.Pending && r.CarrierId != carrierId)
    .CountAsync();

var enabledAfterThisDisable = enabledCount - 1 - pendingRequestsForOtherCarriers;
```

This ensures at least one carrier remains enabled after all pending requests.

### Blocking Conditions
- Cannot disable the only enabled carrier
- Cannot disable carrier with pending shipments
- Cannot disable carrier with pending settlements

---

## Resilience Configuration

HTTP clients configured with Microsoft.Extensions.Http.Resilience:

- **Retry**: 3 attempts, 2s delay with jitter
- **Circuit Breaker**: 30s sampling, 50% failure ratio, 5 min throughput, 30s break

---

## Intentional Design Decisions

### Cache Key Not Include Origin/Destination
The mock services only accept package weight/dimensions - origin/destination are API contract only and don't affect rates from mocks.

### Enable Endpoint Has No Validation
Designed as quick admin "switch" - re-enabling is always safe.

### Admin Authorization (X-Role Header)
API endpoints use an `X-Role` header to distinguish admins from regular users:
- `X-Role: Admin` - full access (can disable carriers, approve disable requests)
- `X-Role: User` - read-only + can request carrier disable

This is a **demonstration auth pattern**, not production security. Real deployments would use JWT, OAuth2, or similar.

### Carrier Configuration
The system stores carrier endpoint URLs but not API keys or credentials:
- Endpoint URLs point to mock services (local Proof of Concept)
- Real carrier APIs would require credential storage (API keys, secrets, etc.)
- Current design demonstrates integration capability without external dependencies

### Local Mock Services
FedEx, DHL, and UPS have local mock implementations:
- Simulate carrier API responses without external network calls
- Rates are calculated from package weight/dimensions
- Response format mirrors real carrier APIs for demonstration

---

For full technical details for AI agents, see [AGENTS.md](../AGENTS.md).