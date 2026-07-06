# Edge360 — Self-Hosted Family Safety Platform

Edge360 is a self-hosted family safety and location-intelligence platform: real-time location
sharing, family groups, geofencing with arrival/departure alerts, safety (SOS) workflows, and
historical tracking. This repository currently contains the **production-grade backend
foundation** — the API spine that the web dashboard and mobile apps build on.

> Status: Backend complete (auth, groups, location, geofencing + events, SOS, real-time,
> driving intelligence, multi-channel notifications, **admin console + data retention**) plus a
> Blazor WebAssembly web dashboard (live map, members, activity feed, SOS, places, driving, admin).
> Docker, CI, tests included. Mobile apps are the remaining roadmap item.

## Tech stack

| Concern        | Choice                                   |
|----------------|------------------------------------------|
| API            | ASP.NET Core 8 (Web API + controllers)   |
| Web dashboard  | Blazor WebAssembly SPA + Leaflet map (vendored) |
| Architecture   | Clean / layered (Domain/Application/Infrastructure/Api) |
| Persistence    | PostgreSQL via EF Core 8 (Npgsql)        |
| Real-time      | SignalR (`/hubs/location`, `/hubs/events`) |
| Auth           | JWT access tokens + rotating refresh tokens (PBKDF2 password hashing) |
| Validation     | FluentValidation                         |
| Logging        | Serilog (structured)                     |
| Caching/scale  | Redis (provisioned; backplane-ready)     |
| Tests          | xUnit + FluentAssertions (SQLite in-memory for integration) |
| Deployment     | Docker + Docker Compose                  |

## Repository layout

```
src/server/
  Edge360.Domain/          Entities, enums, value objects, domain logic (no dependencies)
  Edge360.Application/     Use-case services, DTOs, abstractions, validators
  Edge360.Infrastructure/ EF Core DbContext, JWT, password hashing, auditing, migrations
  Edge360.Api/            Controllers, SignalR hubs, middleware, DI wiring, Program.cs
src/web/
  Edge360.Web/            Blazor WebAssembly dashboard (HTTP + SignalR client of the API)
tests/
  Edge360.Domain.Tests/         Pure unit tests
  Edge360.Application.Tests/     Service tests (SQLite in-memory)
  Edge360.Api.IntegrationTests/  End-to-end HTTP tests (WebApplicationFactory)
docker/                    Dockerfile + docker-compose (api + postgres + redis)
docs/                      Architecture and API reference
.github/workflows/         CI pipeline
```

## Running locally

### Option A — Docker Compose (recommended)

```bash
cd docker
docker compose up --build
```

This starts PostgreSQL, Redis, the API, and the web dashboard. The API applies EF Core migrations
automatically on startup. Once up:

- Web dashboard: http://localhost:8081
- API health check: http://localhost:8080/health
- Swagger UI (Development env): set `ASPNETCORE_ENVIRONMENT=Development` to expose it.

The web container reads the browser-facing API URL from `API_BASE_URL` (default
`http://localhost:8080`), injected into the published app at startup.

### Option B — dotnet CLI

Requires the .NET 8 SDK and a reachable PostgreSQL (or run just the `db` service from compose).

```bash
# from the repo root
dotnet build Edge360.sln
dotnet run --project src/server/Edge360.Api
```

Configure the connection string and JWT key via `appsettings.json` or environment variables:

```bash
export ConnectionStrings__Default="Host=localhost;Port=5432;Database=edge360;Username=edge360;Password=edge360"
export Jwt__SigningKey="a-long-random-secret-at-least-32-characters"
```

## Configuration

| Setting                        | Env var                          | Notes                                  |
|--------------------------------|----------------------------------|----------------------------------------|
| DB connection                  | `ConnectionStrings__Default`     | PostgreSQL connection string           |
| JWT signing key                | `Jwt__SigningKey`                | **Required in prod**, min 32 chars     |
| JWT issuer/audience            | `Jwt__Issuer`, `Jwt__Audience`   |                                        |
| Access token lifetime          | `Jwt__AccessTokenMinutes`        | Default 15                             |
| CORS allowed origins           | `Cors__AllowedOrigins__0`, ...   | Empty = allow all (dev only)           |

## Tests

```bash
dotnet test Edge360.sln
```

67 tests covering domain logic, application services (incl. the driving analyzer, notification
dispatcher, and admin service), and end-to-end HTTP flows.

## Database migrations

```bash
# add a migration
dotnet ef migrations add <Name> \
  --project src/server/Edge360.Infrastructure \
  --startup-project src/server/Edge360.Api \
  --output-dir Persistence/Migrations
```

Migrations are applied automatically at startup (outside the `Testing` environment).

## API overview

See [docs/API.md](docs/API.md) for the full reference. Quick smoke test:

```bash
# register
curl -s localhost:8080/api/auth/register -H 'Content-Type: application/json' \
  -d '{"email":"a@example.com","password":"password123","displayName":"Alice"}'
# -> { accessToken, refreshToken, user }
```

## Architecture

See [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md).

## Web dashboard

A **Blazor WebAssembly** single-page app (`src/web/Edge360.Web`) that consumes the API over HTTP
and subscribes to the SignalR hubs — the same way the mobile apps will. Features: email
auth (login/register with JWT + silent refresh), group create/join and selection, a **live map**
(Leaflet, vendored locally for self-hosting) showing members with real-time updates, an activity
feed with live events and an **SOS** button, **places** management with click-to-place
(role-gated to Guardians/Admins), and a **driving** page (trips, scores, route view).

Run it standalone against a running API:

```bash
dotnet run --project src/web/Edge360.Web   # serves the SPA; configure API URL in wwwroot/appsettings.json
```

The map library is vendored under `wwwroot/lib/leaflet` (no CDN dependency); map tiles default to
OpenStreetMap and can be pointed at a self-hosted tile server.

## Admin console

Platform administrators (`SystemRole.Administrator`) get an oversight surface at `/api/admin/*`
and a role-gated **Admin** page in the web app: system stats, **user management** (promote/demote,
activate/deactivate), **group oversight**, an **audit-log** browser, and **data-retention** policy
with an on-demand purge (a background job runs it on a schedule when `Retention:Enabled` is true).

Bootstrap the first admin without touching the database via the `AdminSeed` config section
(`AdminSeed:Email` / `AdminSeed:Password`) — an admin is created on startup if it doesn't exist.

## Notifications

Safety events fan out to recipients across pluggable channels. Triggers: **SOS** (all members),
**arrival/departure**, **low battery** (raised on ingest when the level crosses the threshold),
and **offline device** (a background monitor flags devices that stop reporting) — these reach
group Guardians/Admins. Each delivery is recorded as an `Alert` with its outcome; a user reads
their own at `GET /api/notifications`.

Channels (each enabled when configured, all implementing `INotificationChannel`):

| Channel | Config section | Notes |
|---------|----------------|-------|
| Logging | `LoggingNotifications` | On by default; structured-log + audit trail |
| Email   | `Email` (SMTP)         | Enabled when `Email:Host` is set |
| Push    | `Webhook`              | HTTP POST to a webhook (ntfy/Gotify/custom); enabled when `Webhook:Url` is set |

Tunables live under `Notifications` (low-battery threshold, offline window, scan interval).

## Driving intelligence

Trips are reconstructed from the location stream and scored. `POST /api/driving/analyze` rebuilds
trips for the caller; the engine (`DrivingAnalyzer`) detects hard braking, harsh acceleration,
harsh cornering, and speeding, then computes a 0–100 score. Query trips, trip detail (with route),
and an aggregate score via `/api/driving/*`. Thresholds are configurable under the `Driving`
section. See [docs/API.md](docs/API.md#driving-intelligence-auth-required).

## Roadmap (not yet implemented)

- .NET MAUI mobile apps (Android/iOS) with battery-aware background tracking
- Redis SignalR backplane for horizontal scale
- Admin console (user/group oversight, retention policy, audit browser)

## License

Original work. Not derived from any proprietary system.
