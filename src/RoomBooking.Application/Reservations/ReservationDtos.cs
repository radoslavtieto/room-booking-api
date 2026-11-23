namespace RoomReservations.Application.Reservations;

public sealed record CreateReservationRequest(
    Guid RoomId,
    DateTimeOffset Start,
    DateTimeOffset End,
    string Title);

public sealed record ReservationResponse(
    Guid Id,
    Guid RoomId,
    DateTimeOffset Start,
    DateTimeOffset End,
    string Title,
    DateTimeOffset CreatedAt);
