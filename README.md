# Tripwalaah Location Service

.NET 10 microservice for Tripwalaah locations using **Controller → Service → Repository**, backed by the same **MongoDB** database as the main Tripwalaah API.

## Architecture

```text
Controllers / SignalR Hub  →  Services  →  Repositories / Presence Store  →  MongoDB
```

| Project | Responsibility |
|---------|----------------|
| `Api` | Controllers, `TripHub` (SignalR), CORS, OpenAPI |
| `Application` | DTOs + location/live-update contracts |
| `Domain` | `Location` entity + GeoJSON point |
| `Infrastructure` | MongoDB, Redis live cache, Kafka producer/consumer |

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
| `GET` | `/api/trips/{tripId}/live/presence` | Connected members snapshot |
| `GET` | `/api/trips/{tripId}/live/locations` | Live locations from Redis |
| `POST` | `/api/trips/{tripId}/live/status` | Broadcast trip status via SignalR + Kafka |
| `POST` | `/api/trips/{tripId}/live/location` | Broadcast location (SignalR + Redis + Kafka) |

## Redis live locations

Every `UpdateLocation` / live location broadcast is saved to Redis:

| Key | Purpose |
|-----|---------|
| `tripwalaah:live:{tripId}:{userId}` | Latest location JSON (TTL, default 900s) |
| `tripwalaah:live:trip:{tripId}` | Hash of all members’ latest locations |

Uses the same `REDIS_URL` style as Tripwalaah Node (`redis://localhost:6379/0`).

## Kafka

Initialized on startup (`KafkaInitializerHostedService`):
- validates broker connectivity
- ensures topics exist:
  - `tripwalaah.trip.live-location`
  - `tripwalaah.trip.events`

Publishes envelopes:

```json
{
  "eventType": "location.updated",
  "occurredAt": "...",
  "source": "tripwalaah-location-service",
  "data": { "...live location..." }
}
```

Optional consumer: set `KAFKA_ENABLE_CONSUMER=true`.

## SignalR live trip updates

Hub URL: `http://localhost:5000/hubs/trip`

Flow for live GPS: **SignalR broadcast → Redis save → Kafka publish**.

### Hub methods (client → server)

| Method | Payload | Description |
|--------|---------|-------------|
| `JoinTrip` | `{ tripId, userId, displayName? }` | Join trip group + receive presence |
| `LeaveTrip` | `tripId` | Leave trip group |
| `UpdateLocation` | `{ tripId, latitude, longitude, speedKmh?, heading? }` | Push live GPS to trip members |
| `GetPresence` | `tripId` | Request current members snapshot |

### Client events (server → client)

| Event | Payload |
|-------|---------|
| `MemberJoined` | `{ tripId, userId, displayName, timestamp }` |
| `MemberLeft` | `{ tripId, userId, displayName, timestamp }` |
| `LocationUpdated` | `{ tripId, userId, latitude, longitude, speedKmh, heading, timestamp }` |
| `TripStatusUpdated` | `{ tripId, status, message, triggeredByUserId, timestamp }` |
| `PresenceSnapshot` | `{ tripId, members[], timestamp }` |
| `Error` | `{ error }` |

### JS client example

```js
import * as signalR from "@microsoft/signalr";

const connection = new signalR.HubConnectionBuilder()
  .withUrl("http://localhost:5000/hubs/trip")
  .withAutomaticReconnect()
  .build();

connection.on("LocationUpdated", (update) => {
  console.log("live location", update);
});

connection.on("MemberJoined", (member) => console.log("joined", member));
connection.on("MemberLeft", (member) => console.log("left", member));
connection.on("PresenceSnapshot", (snap) => console.log("presence", snap));
connection.on("TripStatusUpdated", (status) => console.log("status", status));

await connection.start();
await connection.invoke("JoinTrip", {
  tripId: "TRIP_ID",
  userId: "USER_ID",
  displayName: "Lav"
});

// call periodically from device GPS
await connection.invoke("UpdateLocation", {
  tripId: "TRIP_ID",
  latitude: 26.9124,
  longitude: 75.7873,
  speedKmh: 42,
  heading: 180
});
```

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
| `CORS_ALLOWED_ORIGINS` | Comma-separated frontend origins (required for SignalR browser clients) |
| `API_PREFIX` | `/api` |
| `SIGNALR_ENABLED` | Feature flag marker (`true` by default in docs) |
| `REDIS_URL` | Redis connection (`redis://localhost:6379/0`) |
| `REDIS_ENABLED` | Enable Redis live-location cache |
| `KAFKA_BOOTSTRAP_SERVERS` | Kafka brokers (`localhost:9092`) |
| `KAFKA_ENABLED` | Enable Kafka producer + initializer |

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
# API:   http://localhost:5000
# Mongo: mongodb://localhost:27017/tripwalaah
# Redis: redis://localhost:6379/0
# Kafka: localhost:9092
```

### Azure Container Registry (CI)

GitHub Actions workflow [`.github/workflows/azure-acr.yml`](.github/workflows/azure-acr.yml) builds the image on every PR and pushes to Azure ACR on `main` / `v*.*.*` tags.

Configure these in the GitHub repo settings before the first push:

| Kind | Name | Example |
|------|------|---------|
| Variable (or Secret) | `ACR_LOGIN_SERVER` | `myregistry.azurecr.io` |
| Secret | `ACR_USERNAME` | ACR admin user or service principal appId |
| Secret | `ACR_PASSWORD` | ACR admin password or service principal secret |

`ACR_LOGIN_SERVER` should be under **Variables** (not Secrets). After setting values, re-run **Build and Push to Azure ACR** from the Actions tab.

Image: `{ACR_LOGIN_SERVER}/tripwalaah-location-service` (tags: `latest`, short SHA, semver when tagged).

## Tests

```bash
dotnet test Tripwalaah.LocationService.slnx
```
