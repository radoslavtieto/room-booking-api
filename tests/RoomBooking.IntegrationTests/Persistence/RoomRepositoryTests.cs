using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using RoomReservations.Domain.Entities;
using RoomReservations.Infrastructure.Persistence;
using RoomReservations.Infrastructure.Persistence.Repositories;

namespace RoomBooking.IntegrationTests.Persistence;

public sealed class RoomRepositoryTests : IAsyncLifetime
{
    private readonly SqliteConnection connection = new("Data Source=:memory:");
    private AppDbContext dbContext = null!;

    public async Task InitializeAsync()
    {
        await connection.OpenAsync();
        dbContext = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(connection)
            .Options);
        await dbContext.Database.EnsureCreatedAsync();
    }

    public async Task DisposeAsync()
    {
        await dbContext.DisposeAsync();
        await connection.DisposeAsync();
    }

    [Fact]
    public async Task AddAsync_persists_room_and_get_by_id_returns_it()
    {
        var repository = new RoomRepository(dbContext);
        var room = Room.Create("Meeting room", 8);

        await repository.AddAsync(room);

        var result = await repository.GetByIdAsync(room.Id);

        Assert.NotNull(result);
        Assert.Equal(room.Id, result.Id);
        Assert.Equal("Meeting room", result.Name);
        Assert.Equal(8, result.Capacity);
    }

    [Fact]
    public async Task GetByIdAsync_returns_null_when_room_does_not_exist()
    {
        var repository = new RoomRepository(dbContext);

        var result = await repository.GetByIdAsync(Guid.NewGuid());

        Assert.Null(result);
    }

    [Fact]
    public async Task GetAllAsync_returns_rooms_ordered_by_name()
    {
        var repository = new RoomRepository(dbContext);
        await repository.AddAsync(Room.Create("Zulu", 2));
        await repository.AddAsync(Room.Create("Alpha", 4));

        var result = await repository.GetAllAsync();

        Assert.Equal(new[] { "Alpha", "Zulu" }, result.Select(room => room.Name));
    }
}
