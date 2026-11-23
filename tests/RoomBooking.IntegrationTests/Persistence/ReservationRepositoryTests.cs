using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using RoomReservations.Domain.Entities;
using RoomReservations.Domain.ValueObjects;
using RoomReservations.Infrastructure.Persistence;
using RoomReservations.Infrastructure.Persistence.Repositories;

namespace RoomBooking.IntegrationTests.Persistence;

public sealed class ReservationRepositoryTests : IAsyncLifetime
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
    public async Task Repository_persists_rooms_and_queries_overlapping_reservations()
    {
        var reservationIndexes = dbContext.Model.FindEntityType(typeof(Reservation))!.GetIndexes();
        Assert.Contains(reservationIndexes, index => index.Properties
            .Select(property => property.Name)
            .SequenceEqual(new[] { "RoomId", "Start", "End" }));

        var room = Room.Create("Meeting room", 8);
        var roomRepository = new RoomRepository(dbContext);
        var reservationRepository = new ReservationRepository(dbContext);
        await roomRepository.AddAsync(room);

        var start = new DateTimeOffset(2026, 11, 24, 12, 0, 0, TimeSpan.Zero);
        var reservation = Reservation.Create(
            room.Id,
            TimeRange.Create(start, start.AddHours(1)),
            "Planning",
            new FixedTimeProvider(start));

        Assert.Null(await reservationRepository.AddIfNoOverlapAsync(reservation));
        Assert.True(await reservationRepository.HasOverlappingAsync(
            room.Id,
            TimeRange.Create(start.AddMinutes(30), start.AddHours(2))));
        var conflict = await reservationRepository.AddIfNoOverlapAsync(Reservation.Create(
            room.Id,
            TimeRange.Create(start.AddMinutes(30), start.AddHours(2)),
            "Conflicting",
            new FixedTimeProvider(start)));
        Assert.NotNull(conflict);
        Assert.Equal(reservation.Id, conflict.Id);

        var overlaps = await reservationRepository.GetOverlappingAsync(
            room.Id,
            TimeRange.Create(start.AddMinutes(30), start.AddHours(2)));
        Assert.Single(overlaps);
        Assert.Equal(room.Id, (await roomRepository.GetByIdAsync(room.Id))?.Id);
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
