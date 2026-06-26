# Edge360 — Self-Hosted Family Safety Platform

Edge360 is a self-hosted family safety and location-intelligence platform: real-time location
sharing, family groups, geofencing with arrival/departure alerts, safety (SOS) workflows, and
historical tracking. This repository currently contains the **production-grade backend
foundation** — the API spine that the web dashboard and mobile apps build on.

> Status: Backend foundation complete (auth, groups, location, geofencing + events, SOS,
> real-time, Docker, CI, tests). Web dashboard, mobile apps, and driving-intelligence are on the
> roadmap below.

## Tech stack

| Concern        | Choice                                   |
|----------------|------------------------------------------|
| API            | ASP.NET Core 8 (Web API + controllers)   |
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

This starts PostgreSQL, Redis, and the API. The API applies EF Core migrations automatically on
startup. Once up:

- Health check: http://localhost:8080/health
- Swagger UI (Development env): set `ASPNETCORE_ENVIRONMENT=Development` to expose it.

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

36 tests covering domain logic, application services, and end-to-end HTTP flows.

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

## Roadmap (not yet implemented)

- Driving intelligence (trip detection, scoring, hard-braking) — abstractions in place
- Push / email notification transports (alert records + `IRealtimeNotifier` abstraction exist)
- Blazor web dashboard with live map
- .NET MAUI mobile apps (Android/iOS) with battery-aware background tracking
- Redis SignalR backplane for horizontal scale
- Admin console (user/group oversight, retention policy, audit browser)

## License

Original work. Not derived from any proprietary system.
