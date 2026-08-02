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

Configure these on the GitHub **Environment** named `production`
(**Settings → Environments → production**), not only under repository Actions secrets:

| Kind | Name | Example |
|------|------|---------|
| Variable (or Secret) | `ACR_LOGIN_SERVER` | `myregistry.azurecr.io` |
| Secret | `ACR_USERNAME` | ACR admin user or service principal appId |
| Secret | `ACR_PASSWORD` | ACR admin password or service principal secret |

After setting values, re-run **Build and Push to Azure ACR** from the Actions tab.

Image: `{ACR_LOGIN_SERVER}/tripwalaah-location-service` (tags: `latest`, short SHA, semver when tagged).

### Deploy (pay-as-you-go)

Use **Azure Container Apps Consumption** — scales to **0** when idle, so you pay mainly for request time.

Cheapest first deploy: image from ACR + your existing MongoDB (e.g. Atlas). Redis/Kafka stay **off** (`REDIS_ENABLED=false`, `KAFKA_ENABLED=false`).

#### Option A — one command (local Azure CLI)

```bash
az login
export ACR_NAME=myregistry          # short name (before .azurecr.io)
export RESOURCE_GROUP=tripwalaah-rg
export LOCATION=eastus
export MONGODB_URI='mongodb+srv://USER:PASS@cluster/tripwalaah'
./scripts/deploy-container-app.sh
```

Then open the printed `https://…/health` URL.

#### Option B — GitHub Actions

ACR push already works via Environment **production** (`ACR_*`). Container Apps deploy uses the Azure Portal continuous-deploy repo secrets (`LOCATIONSERVICE_*`) when present.

1. **One-time OIDC fix** (required for repos created after 2026-07-15 — immutable GitHub subject):

```bash
az login
# Optional if auto-discover fails:
# export APP_ID=<value of LOCATIONSERVICE_AZURE_CLIENT_ID>
./scripts/fix-oidc-federated-credential.sh
```

This adds an Entra federated credential for:
`repo:bihiya@55905431/tripwalaah-location-service@1319416454:ref:refs/heads/main`

2. Ensure repo secrets exist (Azure Portal usually created these):

| Secret | Notes |
|--------|--------|
| `LOCATIONSERVICE_AZURE_CLIENT_ID` | Or `AZURE_CLIENT_ID` |
| `LOCATIONSERVICE_AZURE_TENANT_ID` | Or `AZURE_TENANT_ID` |
| `LOCATIONSERVICE_AZURE_SUBSCRIPTION_ID` | Or `AZURE_SUBSCRIPTION_ID` |
| `LOCATIONSERVICE_REGISTRY_USERNAME` | Or `ACR_USERNAME` |
| `LOCATIONSERVICE_REGISTRY_PASSWORD` | Or `ACR_PASSWORD` |
| `MONGODB_URI` | Only required for first-time app create |

Defaults (override with repo variables if needed): ACR `tripalaahacr.azurecr.io`, RG `tripwalaah-location-microservice`, app `location-service`.

3. Run **Deploy to Azure Container Apps** (workflow_dispatch), or push to `main` after an ACR build succeeds.

#### Verify in Azure Portal

**Container Apps** → `tripwalaah-location` → **Application Url** → open `/health`.

## Tests

```bash
dotnet test Tripwalaah.LocationService.slnx
```
