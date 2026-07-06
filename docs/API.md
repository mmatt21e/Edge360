# Edge360 API Reference

Base URL: `/api`. All responses are JSON. Errors use RFC 7807 `application/problem+json`.
Authenticated endpoints require `Authorization: Bearer <accessToken>`.

## Auth

### POST /api/auth/register
Body: `{ "email", "password" (min 8), "displayName" }`
→ `200 { accessToken, accessTokenExpiresAt, refreshToken, user { id, email, displayName, systemRole } }`
Errors: `409` duplicate email, `400` validation.

### POST /api/auth/login
Body: `{ "email", "password" }` → `200` same shape as register. `401` on bad credentials.

### POST /api/auth/refresh
Body: `{ "refreshToken" }` → `200` new token pair (old refresh token is rotated/revoked).
`401` if invalid/expired/already-rotated.

## Groups  *(auth required)*

### GET /api/groups
List groups the caller belongs to → `200 [ { id, name, inviteCode, role, memberCount, createdAt } ]`

### POST /api/groups
Body: `{ "name" }` → `200` group (caller becomes `Admin`).

### POST /api/groups/join
Body: `{ "inviteCode" }` → `200` group (caller becomes `Member`). `404` unknown code, `409` already a member.

### GET /api/groups/{groupId}/members
→ `200 [ { userId, displayName, email, role, locationSharingEnabled, joinedAt } ]`. `403` if not a member.

## Location  *(auth required)*

### POST /api/location
Body: `{ "latitude", "longitude", "accuracyMeters"?, "speedMps"?, "heading"?, "batteryLevel"?, "deviceId"?, "recordedAt"? }`
→ `200` location DTO. Broadcasts to group members and evaluates geofences.

### GET /api/location/latest?groupId={id}
→ `200 [ { userId, displayName, latitude, longitude, accuracyMeters, speedMps, batteryLevel, recordedAt } ]`
Latest point per actively-sharing member.

### GET /api/location/history?groupId={id}&subjectUserId={id}&from=&to=&page=1&pageSize=100
→ `200 { items: [...], page, pageSize, totalCount }` (paged, newest first).

## Geofences (Places)  *(auth required)*

### GET /api/geofences?groupId={id}
→ `200 [ placeDto ]`. Any member may read.

### POST /api/geofences  *(Guardian+)*
Body: `{ "groupId", "name", "latitude", "longitude", "radiusMeters", "notifyOnArrival"?, "notifyOnDeparture"? }`
→ `200` placeDto.

### PUT /api/geofences/{placeId}  *(Guardian+)*
Body: `{ "name", "latitude", "longitude", "radiusMeters", "notifyOnArrival", "notifyOnDeparture" }` → `200` placeDto.

### DELETE /api/geofences/{placeId}  *(Guardian+)*
→ `204`.

`placeDto`: `{ id, groupId, name, latitude, longitude, radiusMeters, notifyOnArrival, notifyOnDeparture }`

## Events  *(auth required)*

### GET /api/events?groupId={id}&page=1&pageSize=50
→ `200 { items: [ eventDto ], page, pageSize, totalCount }` (newest first).

### POST /api/events/sos
Body: `{ "groupId", "latitude"?, "longitude"?, "message"? }` → `200` eventDto (severity `Critical`).
Creates alert records for other group members.

### POST /api/events/{eventId}/acknowledge
→ `200` eventDto with `acknowledged: true`.

`eventDto`: `{ id, groupId, subjectUserId, type, severity, message, placeId, latitude, longitude, occurredAt, acknowledged }`
`type` ∈ { Arrival, Departure, Sos, LowBattery, OfflineDevice, Speeding, CrashSuspected }

## Notifications  *(auth required)*

Safety events (SOS, arrival, departure, low battery, offline device) are delivered to recipients
over every enabled channel (logging, email, webhook/push); each attempt is recorded as an alert.

### GET /api/notifications?page=1&pageSize=50
→ `200 { items: [ alertDto ], page, pageSize, totalCount }` — the caller's alerts, newest first.

`alertDto`: `{ id, eventId, eventType, severity, message, channel, delivered, failureReason, occurredAt }`
`channel` ∈ { log, email, push }. SOS reaches all other members; other types reach Guardians/Admins.

## Driving intelligence  *(auth required)*

### POST /api/driving/analyze
Body: `{ "from"?, "to"? }` → `200 { tripsCreated, trips: [ tripDto ] }`.
Reconstructs and scores the caller's trips from stored location history in the range
(replacing any previously-computed trips that start in that window).

### GET /api/driving/trips?subjectUserId=&from=&to=&page=1&pageSize=50
→ `200 { items: [ tripDto ], page, pageSize, totalCount }` (newest first).
Omit `subjectUserId` for your own trips; supply it to view a member who shares a group with you
(`403` otherwise).

### GET /api/driving/trips/{tripId}
→ `200 { trip: tripDto, events: [ drivingEventDto ], route: [[lat, lon], ...] }`.

### GET /api/driving/score?subjectUserId=&from=&to=
→ `200 { userId, tripCount, totalDistanceMeters, averageScore, hardBrakingCount, hardAccelerationCount, harshCorneringCount, speedingCount }`

`tripDto`: `{ id, userId, startedAt, endedAt, distanceMeters, durationSeconds, maxSpeedMps, averageSpeedMps, hardBrakingCount, hardAccelerationCount, harshCorneringCount, speedingCount, score }`
`drivingEventDto`: `{ id, type, occurredAt, latitude, longitude, magnitude }`
`type` ∈ { HardBraking, HardAcceleration, HarshCornering, Speeding }. Score is 0–100 (100 = flawless).

## Admin  *(auth required — `Administrator` system role; others get 403)*

### GET /api/admin/stats
→ `200 { userCount, activeUserCount, groupCount, deviceCount, locationPointCount, eventsLast24h, tripCount }`

### GET /api/admin/users?search=&page=1&pageSize=50
→ `200 { items: [ adminUserDto ], ... }`. `adminUserDto`:
`{ id, email, displayName, systemRole, isActive, groupCount, deviceCount, lastLoginAt, createdAt }`

### POST /api/admin/users/{userId}/role
Body: `{ "systemRole": "User" | "Administrator" }` → `200` adminUserDto.

### POST /api/admin/users/{userId}/active
Body: `{ "isActive": true|false }` → `200` adminUserDto.

### GET /api/admin/groups?page=1&pageSize=50
→ `200 { items: [ { id, name, inviteCode, memberCount, placeCount, createdByUserId, createdAt } ], ... }`

### GET /api/admin/audit?action=&page=1&pageSize=50
→ `200 { items: [ { id, actorUserId, action, targetType, targetId, ipAddress, createdAt } ], ... }`

### GET /api/admin/retention
→ `200 { locationRetentionDays, eventRetentionDays, auditRetentionDays, enabled }`

### POST /api/admin/retention/run
→ `200 { locationPointsDeleted, eventsDeleted, auditLogsDeleted }` — purges data past the windows.

## Real-time (SignalR)

Connect with the access token via query string (`?access_token=...`):

- `/hubs/location` — server event `locationUpdated` (payload: location DTO)
- `/hubs/events` — server event `eventRaised` (payload: event DTO)

Clients are auto-subscribed to a channel per family group on connect.

## Health

### GET /health
→ `200` "Healthy" when the app and database are reachable.
