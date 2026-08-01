# Tripwalaah Location Service

.NET 10 microservice that manages travel locations for Tripwalaah (cities, airports, landmarks, hotels, and stations).

## Stack

- **.NET 10** / ASP.NET Core Minimal APIs
- Clean architecture: `Api` → `Application` → `Domain` ← `Infrastructure`
- **EF Core 10** with **PostgreSQL** (InMemory for local/dev and tests)
- OpenAPI document at `/openapi/v1.json`
- Health check at `/health`
- Docker + docker-compose

## Solution layout

```text
src/
  Tripwalaah.LocationService.Api            # HTTP endpoints
  Tripwalaah.LocationService.Application    # Use cases / DTOs
  Tripwalaah.LocationService.Domain         # Entities
  Tripwalaah.LocationService.Infrastructure # EF Core, persistence
tests/
  Tripwalaah.LocationService.Tests
```

## API

| Method | Path | Description |
|--------|------|-------------|
| `GET` | `/api/v1/locations` | Search (`query`, `countryCode`, `type`, `isActive`, `page`, `pageSize`) |
| `GET` | `/api/v1/locations/{id}` | Get by id |
| `POST` | `/api/v1/locations` | Create |
| `PUT` | `/api/v1/locations/{id}` | Update |
| `DELETE` | `/api/v1/locations/{id}` | Soft-delete (deactivate) |
| `GET` | `/health` | Health check |

### Create example

```bash
curl -X POST http://localhost:8080/api/v1/locations \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Jaipur",
    "city": "Jaipur",
    "country": "India",
    "countryCode": "IN",
    "latitude": 26.9124,
    "longitude": 75.7873,
    "type": "City",
    "region": "Rajasthan",
    "description": "Pink City",
    "timezone": "Asia/Kolkata"
  }'
```

`LocationType` values: `City`, `Airport`, `Landmark`, `Hotel`, `Station`, `Region`.

## Run locally

Prerequisites: [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)

```bash
# Development uses InMemory DB by default (appsettings.Development.json)
dotnet build Tripwalaah.LocationService.slnx
dotnet run --project src/Tripwalaah.LocationService.Api --launch-profile http
```

API listens on `http://localhost:5080` (see `launchSettings.json`).

### PostgreSQL locally

```bash
docker compose up -d postgres
```

Then set:

```json
"Database": { "Provider": "PostgreSQL" },
"ConnectionStrings": {
  "LocationDb": "Host=localhost;Port=5432;Database=tripwalaah_locations;Username=postgres;Password=postgres"
}
```

## Run with Docker

```bash
docker compose up --build
```

Service: `http://localhost:8080`

## Tests

```bash
dotnet test
```

## Configuration

| Key | Description | Default |
|-----|-------------|---------|
| `Database:Provider` | `PostgreSQL` or `InMemory` | `PostgreSQL` |
| `ConnectionStrings:LocationDb` | DB connection string / in-memory name | see `appsettings.json` |
