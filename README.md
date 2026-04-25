# Carrier Rates Query V2

> **⚡ Modernized version** of the original Carrier Rates Query assessment project.

[🔗 Original Submission](https://github.com/vincejorvina/CarrierRatesQuery)

A .NET 8 Web API that aggregates shipping rates from multiple carriers (FedEx, UPS, DHL).

## Quick Start

```bash
git clone <repo-url>
cd CarrierRatesQueryV2
dotnet run --project CarrierRatesQueryV2.AppHost
```

Access at http://localhost:5133

## Features

- Rate query from multiple carriers
- Carrier management with enable/disable
- Business rules for disabling (cannot disable only carrier, check pending shipments/settlements)
- Disable request workflow (request → admin approval)
- In-memory rate caching (2min TTL)
- Retry + circuit breaker for carrier APIs
- Swagger at `/swagger`

## API Endpoints

| Endpoint | Description |
|----------|-------------|
| `POST /api/rates` | Query rates from all enabled carriers |
| `GET /api/carriers` | List carriers |
| `POST /api/carriers` | Create carrier |
| `PATCH /api/carriers/{id}/enable` | Enable carrier (admin) |
| `PATCH /api/carriers/{id}/disable` | Disable carrier immediately (admin) |
| `POST /api/carriers/{id}/disable-requests` | Request carrier disable |
| `PATCH /api/disable-requests/{id}/approve` | Approve disable request (admin) |

## Architecture

- **FastEndpoints** - API framework
- **Strategy Pattern** - Carrier selection
- **Adapter Pattern** - Response normalization
- **Vertical Slice** - Feature organization

## Documentation

- Quick guide: This file
- Technical details: [ARCHITECTURE.md](./ARCHITECTURE.md)
- AI agent instructions: [AGENTS.md](./AGENTS.md)

## Testing

```bash
dotnet test
```