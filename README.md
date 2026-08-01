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
| `Infrastructure` | MongoDB + in-memory trip presence store |

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
| `POST` | `/api/trips/{tripId}/live/status` | Broadcast trip status via SignalR |
| `POST` | `/api/trips/{tripId}/live/location` | Broadcast location via SignalR (server proxy) |

## SignalR live trip updates

Hub URL: `http://localhost:5000/hubs/trip`

Use this so users in the same trip get live location + membership updates.

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
