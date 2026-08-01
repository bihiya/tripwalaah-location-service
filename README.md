# Tripwalaah Location Service

.NET 10 microservice for Tripwalaah locations using **Controller → Service → Repository**, backed by the same **MongoDB** database as the main Tripwalaah API.

## Architecture

```text
Controllers  →  Services  →  Repositories  →  MongoDB (tripwalaah.locations)
```

| Project | Responsibility |
|---------|----------------|
| `Api` | Controllers, CORS, OpenAPI, env loading |
| `Application` | DTOs + `ILocationService` / `LocationAppService` |
| `Domain` | `Location` entity + GeoJSON point |
| `Infrastructure` | MongoDB documents, repository, indexes, seed |

## MongoDB document shape

Collection: `locations` (database: `tripwalaah`)

```json
{
  "_id": "ObjectId",
  "name": "Jaipur",
  "city": "Jaipur",
  "state": "Rajasthan",
  "country": "India",
  "countryCode": "IN",
  "region": "Rajasthan",
  "location": { "type": "Point", "coordinates": [75.7873, 26.9124] },
  "type": "City",
  "description": "Pink City",
  "timezone": "Asia/Kolkata",
  "googlePlaceId": null,
  "isActive": true,
  "createdAt": "2026-08-01T00:00:00Z",
  "updatedAt": "2026-08-01T00:00:00Z"
}
```

`LocationType`: `City`, `Airport`, `Landmark`, `Hotel`, `Station`, `Region`.

## API

Base URL: `http://localhost:5000`  
Prefix: `/api` (same style as Tripwalaah Node API)

| Method | Path | Description |
|--------|------|-------------|
| `GET` | `/api/locations` | Search (`query`, `countryCode`, `city`, `type`, `isActive`, `page`, `pageSize`) |
| `GET` | `/api/locations/{id}` | Get by MongoDB ObjectId |
| `POST` | `/api/locations` | Create |
| `PUT` | `/api/locations/{id}` | Update |
| `DELETE` | `/api/locations/{id}` | Soft-delete (deactivate) |
| `GET` | `/health` | Health check |

### Create example

```bash
curl -X POST http://localhost:5000/api/locations \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Jaipur",
    "city": "Jaipur",
    "country": "India",
    "countryCode": "IN",
    "latitude": 26.9124,
    "longitude": 75.7873,
    "type": "City",
    "state": "Rajasthan",
    "region": "Rajasthan",
    "description": "Pink City",
    "timezone": "Asia/Kolkata"
  }'
```

## Configuration

Copy `.env.example` → `.env` and use the same Mongo vars as the Node API:

```bash
cp .env.example .env
```

Important keys:

| Env var | Purpose |
|---------|---------|
| `PORT` | HTTP port (default `5000`) |
| `MONGODB_URI` | Same URI as Tripwalaah Node (`mongodb://localhost:27017/tripwalaah`) |
| `DB_MAX_POOL_SIZE` / `DB_MIN_POOL_SIZE` | Pool sizing |
| `DB_CONNECT_TIMEOUT` / `DB_SOCKET_TIMEOUT` | Timeouts (ms) |
| `CORS_ALLOWED_ORIGINS` | Comma-separated frontend origins |
| `API_PREFIX` | `/api` |

Do **not** commit real secrets (Atlas passwords, JWT keys, Twilio, OpenAI, etc.).

## Run locally

Prerequisites: [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0), MongoDB on `localhost:27017`

```bash
dotnet build Tripwalaah.LocationService.slnx
dotnet run --project src/Tripwalaah.LocationService.Api --launch-profile http
```

## Docker

```bash
docker compose up --build
# API: http://localhost:5000
# Mongo: mongodb://localhost:27017/tripwalaah
```

## Tests

```bash
dotnet test Tripwalaah.LocationService.slnx
```
