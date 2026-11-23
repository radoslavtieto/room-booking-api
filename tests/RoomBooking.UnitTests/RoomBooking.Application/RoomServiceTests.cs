using RoomReservations.Application.Abstractions;
using RoomReservations.Application.Rooms;
using RoomReservations.Domain.Entities;
using RoomReservations.Domain.ValueObjects;

namespace RoomBooking.UnitTests.RoomBooking.Application;

public class RoomServiceTests
{
    private static readonly DateTimeOffset Start = new(2026, 11, 24, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task GetAvailableAsync_without_range_returns_all_rooms()
    {
        var rooms = new FakeRooms(Room.Create("A", 2), Room.Create("B", 4));
        var reservations = new FakeReservations();
        var service = new RoomService(rooms, reservations);

        var result = await service.GetAvailableAsync();

        Assert.Equal(2, result.Count);
        Assert.Equal(0, reservations.HasOverlapCalls);
    }

    [Fact]
    public async Task GetAvailableAsync_with_range_excludes_rooms_with_overlaps()
    {
        var available = Room.Create("Available", 2);
        var booked = Room.Create("Booked", 4);
        var reservations = new FakeReservations { OverlappingRoomId = booked.Id };
        var service = new RoomService(new FakeRooms(available, booked), reservations);

        var result = await service.GetAvailableAsync(Start, Start.AddHours(1));

        Assert.Equal(new[] { available.Id }, result.Select(room => room.Id));
        Assert.Equal(TimeRange.Create(Start, Start.AddHours(1)), reservations.LastRange);
    }

    [Fact]
    public async Task GetAvailableAsync_rejects_invalid_range()
    {
        var service = new RoomService(new FakeRooms(), new FakeReservations());

        await Assert.ThrowsAsync<ArgumentException>(() => service.GetAvailableAsync(Start, Start));
    }

    private sealed class FakeRooms(params Room[] items) : IRoomRepository
    {
        public Task<Room?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult(items.FirstOrDefault(room => room.Id == id));

        public Task<IReadOnlyList<Room>> GetAllAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Room>>(items);

        public Task AddAsync(Room room, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class FakeReservations : IReservationRepository
    {
        public Guid? OverlappingRoomId { get; init; }
        public TimeRange? LastRange { get; private set; }
        public int HasOverlapCalls { get; private set; }

        public Task<RoomReservations.Domain.Entities.Reservation?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult<RoomReservations.Domain.Entities.Reservation?>(null);

        public Task<IReadOnlyList<RoomReservations.Domain.Entities.Reservation>> GetByRoomIdAsync(Guid roomId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<RoomReservations.Domain.Entities.Reservation>>([]);

        public Task<bool> HasOverlappingAsync(Guid roomId, TimeRange range, CancellationToken cancellationToken = default)
        {
            HasOverlapCalls++;
            LastRange = range;
            return Task.FromResult(roomId == OverlappingRoomId);
        }

        public Task<IReadOnlyList<RoomReservations.Domain.Entities.Reservation>> GetOverlappingAsync(Guid roomId, TimeRange range, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<RoomReservations.Domain.Entities.Reservation>>([]);

        public Task<bool> TryAddIfNoOverlapAsync(RoomReservations.Domain.Entities.Reservation reservation, CancellationToken cancellationToken = default) =>
            Task.FromResult(true);
    }
}
