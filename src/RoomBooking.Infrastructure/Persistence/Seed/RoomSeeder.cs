using Microsoft.EntityFrameworkCore;
using RoomReservations.Domain.Entities;

namespace RoomReservations.Infrastructure.Persistence.Seed;

public static class RoomSeeder
{
    public static async Task SeedAsync(AppDbContext dbContext, CancellationToken cancellationToken = default)
    {
        if (await dbContext.Rooms.AnyAsync(cancellationToken))
            return;

        dbContext.Rooms.AddRange(
            Room.Create("Small Room", 4),
            Room.Create("Conference Room", 10),
            Room.Create("Auditorium", 50));
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
