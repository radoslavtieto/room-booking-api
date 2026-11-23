using RoomReservations.Application.Abstractions;
using RoomReservations.Application.Exceptions;
using RoomReservations.Domain.Entities;

namespace RoomReservations.Application.Reservations;

public sealed class ReservationService(
    IReservationRepository reservations,
    IRoomRepository rooms,
    TimeProvider timeProvider)
{
    public async Task<IReadOnlyList<ReservationResponse>> GetForRoomAsync(
        Guid roomId,
        DateTimeOffset? start = null,
        DateTimeOffset? end = null,
        CancellationToken cancellationToken = default)
    {
        var range = TimeRangeValidator.CreateOptional(start, end);
        if (await rooms.GetByIdAsync(roomId, cancellationToken) is null)
            throw new NotFoundException($"Room '{roomId}' was not found.");

        var items = range is null
            ? await reservations.GetByRoomIdAsync(roomId, cancellationToken)
            : await reservations.GetOverlappingAsync(roomId, range.Value, cancellationToken);

        return items.Select(ToResponse).ToArray();
    }

    public async Task<ReservationResponse> CreateAsync(
        CreateReservationRequest request,
        CancellationToken cancellationToken = default)
    {
        var range = TimeRangeValidator.Create(request.Start, request.End);
        if (await rooms.GetByIdAsync(request.RoomId, cancellationToken) is null)
            throw new NotFoundException($"Room '{request.RoomId}' was not found.");

        var reservation = Reservation.Create(request.RoomId, range, request.Title, timeProvider);
        var conflict = await reservations.AddIfNoOverlapAsync(reservation, cancellationToken);
        if (conflict is not null)
            throw new ConflictException(
                "The room is already reserved for this time range.",
                conflict.Id,
                conflict.Range.Start,
                conflict.Range.End);

        return ToResponse(reservation);
    }

    private static ReservationResponse ToResponse(Reservation reservation) => new(
        reservation.Id,
        reservation.RoomId,
        reservation.Range.Start,
        reservation.Range.End,
        reservation.Title,
        reservation.CreatedAt);
}
