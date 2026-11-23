using Microsoft.EntityFrameworkCore;
using RoomReservations.Application.Abstractions;
using RoomReservations.Domain.Entities;

namespace RoomReservations.Infrastructure.Persistence.Repositories;

public sealed class RoomRepository(AppDbContext dbContext) : IRoomRepository
{
    public Task<Room?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        dbContext.Rooms.FirstOrDefaultAsync(room => room.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Room>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await dbContext.Rooms.OrderBy(room => room.Name).ToListAsync(cancellationToken);

    public async Task AddAsync(Room room, CancellationToken cancellationToken = default)
    {
        dbContext.Rooms.Add(room);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
