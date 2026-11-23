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

    public async Task<Reservation?> AddIfNoOverlapAsync(
        Reservation reservation,
        DateTimeOffset activeCutoff,
        CancellationToken cancellationToken = default)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);

        var conflicts = dbContext.Reservations.Where(existing =>
            existing.RoomId == reservation.RoomId &&
            existing.End > activeCutoff &&
            existing.Start < reservation.End &&
            existing.End > reservation.Start);

        if (await conflicts.AnyAsync(cancellationToken))
        {
            var conflict = await conflicts
                .OrderByDescending(existing => existing.Start)
                .FirstAsync(cancellationToken);
            await transaction.RollbackAsync(cancellationToken);
            return conflict;
        }

        dbContext.Reservations.Add(reservation);
        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return null;
    }
}
