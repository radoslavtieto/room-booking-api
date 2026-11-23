using System.Data;
using Microsoft.EntityFrameworkCore;
using RoomReservations.Application.Abstractions;
using RoomReservations.Domain.Entities;
using RoomReservations.Domain.ValueObjects;

namespace RoomReservations.Infrastructure.Persistence.Repositories;

public sealed class ReservationRepository(AppDbContext dbContext) : IReservationRepository
{
    public Task<Reservation?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        dbContext.Reservations.FirstOrDefaultAsync(reservation => reservation.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Reservation>> GetByRoomIdAsync(Guid roomId, CancellationToken cancellationToken = default) =>
        await dbContext.Reservations
            .Where(reservation => reservation.RoomId == roomId)
            .OrderBy(reservation => reservation.Start)
            .ToListAsync(cancellationToken);

    public Task<bool> HasOverlappingAsync(Guid roomId, TimeRange range, CancellationToken cancellationToken = default) =>
        dbContext.Reservations.AnyAsync(
            reservation => reservation.RoomId == roomId &&
                reservation.Start < range.End && reservation.End > range.Start,
            cancellationToken);

    public async Task<IReadOnlyList<Reservation>> GetOverlappingAsync(Guid roomId, TimeRange range, CancellationToken cancellationToken = default) =>
        await dbContext.Reservations
            .Where(reservation => reservation.RoomId == roomId &&
                reservation.Start < range.End && reservation.End > range.Start)
            .OrderBy(reservation => reservation.Start)
            .ToListAsync(cancellationToken);

    public async Task<bool> TryAddIfNoOverlapAsync(Reservation reservation, CancellationToken cancellationToken = default)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);

        if (await HasOverlappingAsync(reservation.RoomId, reservation.Range, cancellationToken))
        {
            await transaction.RollbackAsync(cancellationToken);
            return false;
        }

        dbContext.Reservations.Add(reservation);
        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return true;
    }
}
