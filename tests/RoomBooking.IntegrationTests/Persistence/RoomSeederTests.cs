using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using RoomReservations.Domain.Entities;
using RoomReservations.Infrastructure.Persistence;
using RoomReservations.Infrastructure.Persistence.Seed;

namespace RoomBooking.IntegrationTests.Persistence;

public sealed class RoomSeederTests : IAsyncLifetime
{
    private readonly SqliteConnection connection = new("Data Source=:memory:");
    private AppDbContext dbContext = null!;

    public async Task InitializeAsync()
    {
        await connection.OpenAsync();
        dbContext = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(connection)
            .Options);
        await dbContext.Database.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        await dbContext.DisposeAsync();
        await connection.DisposeAsync();
    }

    [Fact]
    public async Task SeedAsync_inserts_default_rooms_when_table_is_empty()
    {
        await RoomSeeder.SeedAsync(dbContext);

        var rooms = await dbContext.Rooms.OrderBy(room => room.Capacity).ToListAsync();
        Assert.Equal(
            new[] { ("Small Room", 4), ("Conference Room", 10), ("Auditorium", 50) },
            rooms.Select(room => (room.Name, room.Capacity)));
    }

    [Fact]
    public async Task SeedAsync_does_not_add_defaults_when_any_room_exists()
    {
        var existing = Room.Create("Custom room", 6);
        dbContext.Rooms.Add(existing);
        await dbContext.SaveChangesAsync();

        await RoomSeeder.SeedAsync(dbContext);

        var rooms = await dbContext.Rooms.ToListAsync();
        var room = Assert.Single(rooms);
        Assert.Equal(existing.Id, room.Id);
        Assert.Equal("Custom room", room.Name);
    }
}
