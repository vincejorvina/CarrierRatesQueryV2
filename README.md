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

Aspire dashboard: http://localhost:15074

API directly: http://localhost:5117/swagger

## Features

- Rate query from multiple carriers
- Carrier management with enable/disable
- Business rules for disabling (cannot disable only carrier, check pending shipments/settlements)
- Disable request workflow (request → admin approval)
- In-memory rate caching (2min TTL)
- Retry + circuit breaker for carrier APIs
- Swagger at `/swagger`

## API Endpoints

| Endpoint                                   | Description                           |
|--------------------------------------------|---------------------------------------|
| `POST /api/v1/rates`                          | Query rates from all enabled carriers |
| `GET /api/v1/carriers`                        | List carriers                         |
| `POST /api/v1/carriers`                       | Create carrier                        |
| `PATCH /api/v1/carriers/{id}/enable`          | Enable carrier                        |
| `PATCH /api/v1/carriers/{id}/disable`         | Disable carrier immediately (admin)   |
| `POST /api/v1/carriers/{id}/disable-requests` | Request carrier disable               |
| `PATCH /api/v1/disable-requests/{id}/approve` | Approve disable request (admin)       |

## Architecture

- **FastEndpoints** - API framework
- **Strategy Pattern** - Carrier selection
- **Adapter Pattern** - Response normalization
- **Vertical Slice** - Feature organization

## Carrier API Contracts

The mock carrier APIs conform to the assessment payload specifications:

### FedEx
```json
POST /api/fedex/rates
{
  "origin": { "postalCode": "12345", "countryCode": "US" },
  "destination": { "postalCode": "67890", "countryCode": "US" },
  "package": { "weight": 5, "dimensions": { "length": 10, "width": 5, "height": 5 } }
}
```

### DHL
```json
POST /api/dhl/rates
{
  "from": { "zipCode": "12345", "country": "US" },
  "to": { "zipCode": "67890", "country": "US" },
  "parcel": { "weightKg": 5, "sizeCm": { "length": 10, "width": 5, "height": 5 } }
}
```

### UPS
```json
POST /api/ups/shipping-rates
{
  "shipment": {
    "originPostalCode": "12345",
    "destinationPostalCode": "67890",
    "originCountryCode": "US",
    "destinationCountryCode": "US",
    "weightLbs": 11,
    "dimensionsInches": { "length": 10, "width": 5, "height": 5 }
  }
}
```

Note: Origin/destination are accepted per API contract but do not affect rate calculations in the mock services.

## Documentation

- Quick guide: This file
- Technical details: [docs/ARCHITECTURE.md](./docs/ARCHITECTURE.md)
- Adding carriers: [docs/CARRIER_IMPLEMENTATION.md](./docs/CARRIER_IMPLEMENTATION.md)
- AI agent instructions: [AGENTS.md](./AGENTS.md)

## Testing

```bash
dotnet test
```
