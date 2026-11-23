# Room Booking API

## Prerequisites

- .NET 10 SDK
- No separate database server is needed; the API uses SQLite.

## Run and test

From the repository root:

```sh
dotnet run --project src/RoomBooking.Api
```

The API applies EF migrations and seeds default rooms on startup. Use the base URL printed by `dotnet run` (for example, `http://localhost:5000`).

```sh
dotnet test
```

## Endpoints

Set `BASE` to the URL printed when the API starts. Get a room ID from the first response, then use it in the reservation requests:

```sh
curl "$BASE/api/rooms/available"
curl "$BASE/api/rooms/available?start=2026-11-24T10:00:00Z&end=2026-11-24T11:00:00Z"
curl "$BASE/api/rooms/<roomId>/reservations"
curl -X POST "$BASE/api/rooms/<roomId>/reservations" \
  -H "Content-Type: application/json" \
  -d '{"start":"2026-11-24T10:00:00Z","end":"2026-11-24T11:00:00Z","title":"Planning"}'
```

Errors use `application/problem+json`. A `409 Conflict` response includes the conflicting reservation's `reservationId`, `start`, and `end`.

## Assumptions

- Reservation ranges are half-open (`[start, end)`), normalized to UTC. Send ISO 8601 timestamps with an offset; examples use `Z` (UTC).
- “Available” means no reservation overlaps the requested range. Without a range, all rooms are returned. When filtering, both `start` and `end` are required.
- A reservation conflicts only with an overlapping reservation in the same room. Adjacent bookings are allowed; conflicts return `409`.
