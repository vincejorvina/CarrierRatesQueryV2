# Carrier Rates Query V2 (Modernized)

> **⚡ Modernized version** of the original Carrier Rates Query assessment project. Built with modern .NET 8 architecture, FastEndpoints, Refit, Shouldly, and vertical slice principles.

[🔗 Original Submission](https://github.com/vincejorvina/CarrierRatesQuery)

---

## 🎯 Overview

This is a **modernized, production-ready** version of the original Carrier Rates Query assessment project. The system aggregates shipping rates from multiple carrier APIs (FedEx, UPS, DHL) and returns them in a unified format.

**Key Modernizations:**
- ✅ **FastEndpoints** - Modern, testable API endpoint framework
- ✅ **Refit** - Type-safe HTTP client generation for mock carrier APIs
- ✅ **Shouldly** - Readable test assertions with fluent syntax
- ✅ **FluentValidation** - Centralized request validation
- ✅ **Vertical Slice Architecture** - Organized by feature/domain
- ✅ **Resilience & Retry** - Microsoft.Extensions.Http.Resilience (retry + circuit breaker)
- ✅ **Aspire Integration** - Built-in observability and health checks

---

## 🛠️ Tech Stack

- **.NET 8 Web API** - Target framework
- **FastEndpoints** - Modern, testable API framework (v8.1.0)
- **Refit** - Type-safe HTTP client generation (v6.0.0)
- **FluentValidation** - Centralized request validation (v12.1.1)
- **xUnit + Shouldly** - Testing with readable assertions
- **EF Core InMemory** - In-memory database (v9.0.15)
- **Microsoft.Extensions.Http.Resilience** - Retry & circuit breaker (v10.1.0)
- **CarrierFailureTracker** - In-memory failure tracking for carrier resilience (30s cache)
- **OpenTelemetry** - Observability & tracing (v1.14.0)
- **Aspire** - Distributed application orchestration (v13.1.0)

---

## 📁 Solution Structure

The solution is organized using vertical slice architecture with a Core project pattern:

**Main API Service:**
- `CarrierRatesQueryV2.Api/` - Main API with FastEndpoints
  - `Features/` - Vertical slices organized as `/Features/<Category>/<SpecificFunction>/`
    - Example: `/Features/Carriers/Create/` contains:
      - `Endpoint.cs` - Endpoint definition with DTOs, validators, and logic
      - `Endpoint.http` - Example request file
    - Categories include: `Rates`, `Carriers`, `DisableRequests`
  - `Infrastructure/` - HTTP clients, persistence, and external services

**Core Business Logic Library:**
- `CarrierRatesQueryV2.Core/` - Business logic and domain models
  - `Services/` - Business logic services (RateQueryService, CarrierManagementService, DisableRequestService)
  - `Interfaces/` - Service interfaces, strategy and adapter interfaces
  - `Strategies/` - Strategy pattern implementations (FedEx, UPS, DHL)
  - `Adapters/` - Adapter pattern implementations for carrier responses
  - `DTOs/` - Data transfer objects and request models
  - `Exceptions/` - Custom exception types
  - Can be shared across API and test projects

**Data Layer:**
- `CarrierRatesQueryV2.Data/` - EF Core entities and in-memory database

**Shared Services:**
- `CarrierRatesQueryV2.ServiceDefaults/` - Aspire extensions for resilience, OpenTelemetry, and health checks

**Orchestration:**
- `CarrierRatesQueryV2.AppHost/` - Aspire host to run all services locally

**Mock Carrier APIs:**
- `CarrierRatesQueryV2.MockFedEx/` - Mock FedEx API service
- `CarrierRatesQueryV2.MockUps/` - Mock UPS API service
- `CarrierRatesQueryV2.MockDhl/` - Mock DHL API service

**Testing:**
- `CarrierRatesQueryV2.Tests/` - Unit and integration tests
  - `Core/` - Tests for business logic, strategies, and adapters
  - `Api/` - Tests for API features and endpoints
  - `MockCarrierServices/` - Tests for mock carrier APIs

---

## 🏗️ Architecture

### Design Patterns

**Strategy Pattern:** Dynamic carrier selection via `ICarrierRateStrategy` interface with concrete implementations for FedEx, UPS, and DHL.

**Adapter Pattern:** `ICarrierRateAdapter<T>` interface normalizes carrier-specific API responses into a unified `ShippingRateResponseDto` format.

**Open/Closed Principle:** The system is designed to accept new carriers through extension points—implement the strategy and adapter interfaces without modifying existing code.

**Vertical Slice Architecture:** Each feature is self-contained with its own endpoint, handler, and logic, making it easy to test and maintain.

**Dependency Injection:** Full DI integration using Microsoft.Extensions.DependencyInjection for loose coupling and easy unit testing.

### Core Flow

1. Client sends rate query to API
2. FastEndpoint validates and routes the request
3. Feature endpoint invokes Core service (business logic)
4. Service checks cache first (2-minute TTL)
5. If cache miss, Strategy pattern selects appropriate carrier
6. Adapter normalizes carrier-specific response
7. Response is cached and returned to client

### Business Rules

**Carrier Management:**
- A carrier cannot be disabled if it is the only active carrier remaining
- A carrier with ongoing shipments (pending status) cannot be disabled
- A carrier with pending invoices or settlements cannot be disabled
- Disable reason must be logged (maintenance, contract termination, user request)
- Admin-only approval workflow required for disable requests

**Rate Query:**
- In-memory caching with 2-minute default TTL
- Cache invalidation on carrier endpoint changes
- Retry logic with exponential backoff for carrier API calls

---

## 🚀 Getting Started

### Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) or later
- Git
- (Optional) Visual Studio 2022 or VS Code

### Clone the Repository

```bash
git clone <your-repo-url>
cd CarrierRatesQueryV2
```

### Restore Dependencies

```bash
dotnet restore
```

### Run the Application

#### Option A: Aspire AppHost (Recommended)

```bash
dotnet run --project CarrierRatesQueryV2.AppHost
```

This starts:
- ✅ Main API service
- ✅ Mock carrier APIs (FedEx, UPS, DHL)
- ✅ Aspire dashboard (service discovery + health checks)

#### Option B: Run Services Manually

```bash
# Terminal 1: Mock FedEx
dotnet run --project CarrierRatesQueryV2.MockFedEx

# Terminal 2: Mock UPS
dotnet run --project CarrierRatesQueryV2.MockUps

# Terminal 3: Mock DHL
dotnet run --project CarrierRatesQueryV2.MockDhl

# Terminal 4: Main API
dotnet run --project CarrierRatesQueryV2.Api
```

### API Endpoints

Once running, access the API:

- `/swagger` - Swagger/OpenAPI documentation
- `/health` - Health check (development only)
- `/api/rates` - POST to query shipping rates from all enabled carriers
- `/api/carriers` - GET/POST/PUT/DELETE for carrier CRUD operations
- `/api/carriers/{id}/endpoints` - GET/POST/PUT/DELETE for carrier endpoint management
- `/api/carriers/{id}/disable` - POST to disable a carrier (admin approval workflow)
- `/api/disable-requests` - GET/POST for disable request approval workflow

---

## 📋 API Reference

### Rate Query

**Endpoint:** `POST /api/rates`

**Request:**
```json
{
  "origin": {
    "postalCode": "12345",
    "countryCode": "US"
  },
  "destination": {
    "postalCode": "67890",
    "countryCode": "US"
  },
  "package": {
    "weight": 5,
    "dimensions": {
      "length": 10,
      "width": 5,
      "height": 5
    }
  }
}
```

**Response:**
```json
{
  "carrier": "FedEx",
  "rateOptions": [
    {
      "serviceName": "FedEx Ground",
      "estimatedDelivery": "2024-06-15T00:00:00Z",
      "price": {
        "amount": 12.34,
        "currency": "USD"
      }
    }
  ]
}
```

### Carrier Management

**List All Carriers:** `GET /api/carriers`

**Create Carrier:** `POST /api/carriers`

```json
{
  "name": "New Carrier",
  "slug": "new-carrier",
  "isEnabled": false,
  "endpoints": [
    {
      "operation": "Rates",
      "endpoint": "https://api.new-carrier.com/rates"
    }
  ]
}
```

**Disable Carrier:** `POST /api/carriers/{id}/disable`

```json
{
  "reason": "Contract termination",
  "requestedBy": "admin@example.com"
}
```

---

## 🧪 Testing

### Run All Tests

```bash
dotnet test
```

### Run Specific Test Projects

```bash
# Unit tests
dotnet test CarrierRatesQueryV2.Tests

# Coverage report
dotnet test --collect:"XPlat Code Coverage"
```

### Test Categories

- **Core Tests** (`Core/`) - Tests for business logic, strategies, and adapters
- **API Tests** (`Api/`) - Tests for API features and endpoints
- **Integration Tests** (`Integration/`) - End-to-end API integration tests
- **Mock Carrier Tests** (`MockCarrierServices/`) - Tests for mock carrier APIs

### Example Test (Shouldly Syntax)

```csharp
[Fact]
public void Should_Return_Rates_For_Enabled_Carriers()
{
    var result = await _service.QueryAllEnabledCarriersAsync(request);
    
    result.Should.NotBeNull();
    result.Should.NotBeEmpty();
    
    result.ShouldContain(r => r.Carrier == "FedEx");
    result.ShouldContain(r => r.Carrier == "UPS");
}
```

---

## 📦 Package References

### API Project (`CarrierRatesQueryV2.Api.csproj`)

```xml
<PackageReference Include="FastEndpoints" Version="8.1.0" />
<PackageReference Include="FastEndpoints.Swagger" Version="8.1.0" />
<PackageReference Include="FluentValidation" Version="12.1.1" />
<PackageReference Include="FluentValidation.AspNetCore" Version="11.3.0" />
<PackageReference Include="FluentValidation.DependencyInjectionExtensions" Version="11.3.0" />
<PackageReference Include="Microsoft.EntityFrameworkCore" Version="9.0.15" />
<PackageReference Include="Microsoft.EntityFrameworkCore.InMemory" Version="9.0.15" />
<PackageReference Include="Microsoft.Extensions.Http.Resilience" Version="10.1.0" />
<PackageReference Include="Refit" Version="6.0.0" />
<PackageReference Include="Refit.HttpClientFactory" Version="6.0.0" />
<PackageReference Include="Swashbuckle.AspNetCore" Version="6.6.2" />
```

### Core Project (`CarrierRatesQueryV2.Core.csproj`)

```xml
<PackageReference Include="FluentValidation" Version="12.1.1" />
```

### Test Project (`CarrierRatesQueryV2.Tests.csproj`)

```xml
<PackageReference Include="xunit" Version="2.9.3" />
<PackageReference Include="xunit.runner.visualstudio" Version="3.1.5" />
<PackageReference Include="Shouldly" Version="4.2.1" />
<PackageReference Include="Microsoft.EntityFrameworkCore.InMemory" Version="9.0.15" />
```

---

## 🎁 Bonus Features

- ✅ **Resilience Patterns**: Microsoft.Extensions.Http.Resilience with retry + circuit breaker
- ✅ **Carrier Failure Tracking**: In-memory cache tracks failing carriers to prevent repeated calls
- ✅ **Circuit Breaker**: Automatic failure isolation for carrier APIs
- ✅ **OpenTelemetry**: Distributed tracing and metrics
- ✅ **Health Checks**: `/health` endpoint for load balancer integration
- ✅ **Aspire Dashboard**: Service discovery and health monitoring

---

## 📝 Notes

- **In-Memory Database**: Seeded on startup (no external DB required)
- **Resilience**: Built-in via Aspire ServiceDefaults (retry, circuit breaker, timeout)
- **Mock APIs**: Separate console apps simulating real carrier APIs
- **Caching**: 2-minute TTL with invalidation on endpoint changes
- **Validation**: FluentValidation decorators on all request DTOs

---

## 🔄 Modernization Highlights

This version modernizes the original submission with the following enhancements:

- **FastEndpoints** replaces MVC Controllers for better testability
- **Refit** provides type-safe HTTP client generation
- **Shouldly** offers readable, fluent test assertions
- **FluentValidation** centralizes request validation rules
- **Vertical Slice Architecture** with deep feature folders: `/Features/<Category>/<SpecificFunction>/`
- **Core Project** separates business logic (services, strategies, adapters) from API endpoints
- **Http Resilience** adds retry + circuit breaker to carrier API calls
- **OpenTelemetry** enables full observability and distributed tracing

---

## 📞 Support

For questions or issues with this modernized version, please open an issue in the repository.
