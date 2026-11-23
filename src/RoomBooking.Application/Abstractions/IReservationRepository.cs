using RoomReservations.Domain.Entities;
using RoomReservations.Domain.ValueObjects;

namespace RoomReservations.Application.Abstractions;

public interface IReservationRepository
{
    Task<Reservation?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Reservation>> GetByRoomIdAsync(Guid roomId, CancellationToken cancellationToken = default);
    Task<bool> HasOverlappingAsync(Guid roomId, TimeRange range, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Reservation>> GetOverlappingAsync(Guid roomId, TimeRange range, CancellationToken cancellationToken = default);
    Task<Reservation?> AddIfNoOverlapAsync(Reservation reservation, CancellationToken cancellationToken = default);
}
