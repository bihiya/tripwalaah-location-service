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
- `GET /health` reports `Healthy` only when MongoDB is reachable, so it doubles as a DB connectivity
  check.
- On first startup in Development the API auto-seeds sample locations into `tripwalaah.locations`
  when the collection is empty, and creates geo/text indexes — so search works immediately.

### Running the API

- `dotnet run --project src/Tripwalaah.LocationService.Api --launch-profile http` serves on
  `http://localhost:5000` (the `http` launch profile sets `MONGODB_URI=mongodb://localhost:27017/tripwalaah`).
  You do not need a `.env` file for local dev; `launchSettings.json` provides the defaults.

### Tests & lint

- `dotnet test Tripwalaah.LocationService.slnx` does **not** require MongoDB — the unit/API tests use
  an in-memory fake `ILocationRepository`, so they run without any running service.
- There is no dedicated linter; `dotnet format Tripwalaah.LocationService.slnx --verify-no-changes`
  is used as the formatting/lint gate.
