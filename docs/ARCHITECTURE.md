# Edge360 Architecture

## Layered (clean) architecture

Dependencies point inward; the Domain has no outward dependencies.

```
┌─────────────────────────────────────────────┐
│ Edge360.Api  (controllers, hubs, middleware) │
│   depends on ▼                               │
│ Edge360.Infrastructure (EF Core, JWT, hash)  │
│   depends on ▼                               │
│ Edge360.Application (use cases, DTOs, ports) │
│   depends on ▼                               │
│ Edge360.Domain (entities, value objects)     │
└─────────────────────────────────────────────┘
```

- **Domain** — Pure business types: `User`, `Group`, `GroupMembership`, `LocationPoint`,
  `Place`, `SafetyEvent`, `Alert`, `Device`, `RefreshToken`, `AuditLog`; the `GeoCoordinate`
  value object (Haversine distance); enums. No framework dependencies.
- **Application** — Use-case services (`AuthService`, `GroupService`, `LocationService`,
  `GeofenceService`, `EventService`), DTOs, validators, and **ports** (interfaces) the outer
  layers implement: `IAppDbContext`, `IPasswordHasher`, `ITokenService`, `ICurrentUser`,
  `IRealtimeNotifier`, `IAuditWriter`. Authorization is centralized in `GroupAccess`.
- **Infrastructure** — Adapters: EF Core `AppDbContext` (implements `IAppDbContext`), entity
  configurations, `JwtTokenService`, `Pbkdf2PasswordHasher`, `AuditWriter`, migrations.
- **Api** — HTTP/realtime surface: controllers, SignalR hubs, `CurrentUser`, the SignalR
  `IRealtimeNotifier` adapter, exception→ProblemDetails middleware, validation filter, DI.

## Key flows

### Location ingestion + geofencing
1. `POST /api/location` → `LocationService.RecordAsync`.
2. The previous sample is read, then the new `LocationPoint` is persisted.
3. For each group the member actively shares with, the location is broadcast over SignalR.
4. `GeofenceEvaluator` (pure function) compares previous vs. current position against each
   group `Place` to detect Entered/Exited transitions.
5. Transitions become `SafetyEvent`s (Arrival/Departure) and are pushed over the event hub.

### Driving intelligence
- `DrivingAnalyzer` (pure, in Application) reconstructs trips from a user's ordered location
  stream: it splits on time gaps, derives per-point speed/heading when not reported, and detects
  hard braking, harsh acceleration, harsh cornering, and speeding (debounced), then scores each
  trip 0–100. `DrivingService` persists `Trip` + `DrivingEvent` records and serves trip/score
  queries. Cross-user reads are gated by `GroupAccess.RequireCanViewAsync` (shared-group check).
- Thresholds live in `DrivingThresholds` (config section `Driving`).

### Authentication
- Passwords hashed with PBKDF2 (HMAC-SHA256, 100k iterations), stored as `iter.salt.hash`.
- Login/refresh issue a short-lived JWT plus an opaque refresh token (stored **hashed**).
- Refresh **rotates**: the presented token is revoked and linked to its replacement, enabling
  reuse/theft detection.

### Authorization
- Every group-scoped operation routes through `GroupAccess.RequireMembershipAsync` /
  `RequireRoleAsync`. Roles: `Admin (0) < Guardian (1) < Member (2)` (lower = more privilege).
- Mutating geofences requires Guardian or higher.

## Data model notes

- `location_points` is the high-volume time-series table, indexed by `(UserId, RecordedAt)` and
  intended for time-based partitioning and retention jobs.
- `audit_logs` is append-only.
- Unique constraints: `users.NormalizedEmail`, `groups.InviteCode`, `(GroupId, UserId)` on
  memberships, `refresh_tokens.TokenHash`.
- `DateTimeOffset` maps natively on PostgreSQL; under the SQLite test provider it is converted
  to an order-preserving binary form (provider-detected in `AppDbContext.ConfigureConventions`).

## Cross-cutting

- **Errors**: application exceptions (`NotFound`, `Conflict`, `Forbidden`, `Unauthorized`,
  `AppValidation`) map to RFC 7807 ProblemDetails.
- **Validation**: a global action filter runs registered FluentValidation validators per request.
- **Observability**: Serilog structured logging + request logging; health checks at `/health`
  including a DbContext check.
- **Security**: JWT bearer, per-IP rate limiting (global) + an `auth` policy, CORS, audit logging.

## Real-time

- `LocationHub` (`/hubs/location`) and `EventHub` (`/hubs/events`). On connect, a client is
  subscribed to a SignalR group per family group (`group:{id}`). Broadcasts target those groups.
- Redis is provisioned in Compose and is the intended SignalR backplane for multi-instance scale.

## Web dashboard (`src/web/Edge360.Web`)

- **Blazor WebAssembly** SPA — a pure HTTP + SignalR client of the API (no server projects
  referenced; it defines its own client DTOs). This keeps the web surface symmetric with how the
  mobile apps will consume the API.
- **Auth**: tokens persist in browser local storage (`TokenStore`); `JwtAuthStateProvider` decodes
  the JWT into a `ClaimsPrincipal`; an `AuthHeaderHandler` `DelegatingHandler` attaches the bearer
  token and transparently refreshes on a 401 before retrying.
- **State**: `AppState` holds the loaded groups and active group (and exposes role-based flags such
  as `CanManagePlaces`); `RealtimeService` manages the two hub connections and surfaces pushes as
  C# events the pages subscribe to.
- **Map**: `MapInterop` is the map-visualization abstraction (spec §10) over a small Leaflet JS
  module (`wwwroot/js/map.js`); Leaflet is vendored locally so the app has no CDN dependency.
- **Pages**: Login/Register, Groups, Dashboard (live map + members + activity + SOS), Places
  (click-to-place, role-gated), Driving (trips/score/route). Protected routes redirect to login.

## Testing strategy

- **Domain**: pure unit tests (geo math, transitions, invite codes, token state).
- **Application**: services against a real `AppDbContext` on SQLite in-memory, with fakes for
  token/notifier/audit ports.
- **Api**: `WebApplicationFactory` boots the real pipeline in a `Testing` environment with the
  database swapped to SQLite; tests exercise full HTTP journeys.
