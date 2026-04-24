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

## Final Notes

- Do not try to simulate the full pipeline in unit tests
- If HTTP behavior is involved, use integration tests
- Prefer clarity over clever abstractions
- If unable to resolve an issue after a few attempts, or if the infrastructure or architecture has to change, stop and consult the user before going ahead with a fix
