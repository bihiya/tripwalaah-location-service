# AGENTS.md

## Cursor Cloud specific instructions

This is a single .NET 10 microservice (Tripwalaah Location Service). Standard build/test/run
commands live in `README.md`; only the non-obvious, environment-specific notes are captured here.

### Toolchain (already installed in the VM snapshot)

- .NET SDK **10.0.302** is installed at `/usr/share/dotnet` and symlinked to `/usr/local/bin/dotnet`
  (matches `global.json`). The startup update script only runs `dotnet restore` — the SDK itself is
  baked into the snapshot, not reinstalled per run.
- MongoDB **8.0** (server + `mongosh`) is installed natively via apt. Note the repo's
  `docker-compose.yml` pins `mongo:7`; the natively installed 8.0 is compatible with the MongoDB
  3.x driver used here, so either works.

### Running MongoDB (required for the API, NOT for tests)

- The VM has **no systemd**, so `systemctl start mongod` does not work. Start MongoDB manually:
  ```bash
  mongod --dbpath /data/db --bind_ip 127.0.0.1 --port 27017
  ```
  Run it in a background/tmux session (`/data/db` already exists and is writable). Verify with
  `mongosh --quiet --eval "db.adminCommand('ping')"`.
- `GET /health` reports dependency health (Mongo/Redis when enabled).
- On first startup in Development the API auto-seeds sample locations into `tripwalaah.locations`
  when the collection is empty, and creates geo/text indexes — so search works immediately.

### Redis + Kafka (live locations)

- Live GPS updates are saved to Redis (`REDIS_URL`, default `redis://localhost:6379/0`) and published
  to Kafka (`KAFKA_BOOTSTRAP_SERVERS`, default `localhost:9092`).
- If Redis/Kafka are not running locally, either start them via `docker compose up redis kafka -d`
  or disable for a Mongo-only run:
  ```bash
  REDIS_ENABLED=false KAFKA_ENABLED=false dotnet run --project src/Tripwalaah.LocationService.Api --launch-profile http
  ```
- Kafka initializer creates topics `tripwalaah.trip.live-location` and `tripwalaah.trip.events` when
  brokers are reachable; failures are logged as warnings and do not crash the API.

### SignalR

- Hub endpoint: `/hubs/trip`
- Client methods: `JoinTrip`, `LeaveTrip`, `UpdateLocation`, `GetPresence`
- Live update path: SignalR broadcast → Redis save → Kafka publish

### Running the API

- `dotnet run --project src/Tripwalaah.LocationService.Api --launch-profile http` serves on
  `http://localhost:5000` (the `http` launch profile sets Mongo/Redis/Kafka defaults).
  You do not need a `.env` file for local dev; `launchSettings.json` provides the defaults.

### Tests & lint

- `dotnet test Tripwalaah.LocationService.slnx` does **not** require MongoDB/Redis/Kafka — API tests
  disable Redis/Kafka and use fake repositories/caches.
- There is no dedicated linter; `dotnet format Tripwalaah.LocationService.slnx --verify-no-changes`
  is used as the formatting/lint gate.

### Realtime (SignalR) feature

- The API exposes a SignalR hub at `/hubs/trip` plus server-side broadcast REST endpoints under
  `/api/trips/{tripId}/live/*` (documented in `README.md`). There is no browser UI, so to exercise
  the hub end to end use a SignalR client (e.g. a small `Microsoft.AspNetCore.SignalR.Client` console
  app): connect, `JoinTrip`, then POST to `/api/trips/{tripId}/live/status` and confirm the client
  receives the `TripStatusUpdated` event. This path does not require MongoDB (presence is in-memory).
