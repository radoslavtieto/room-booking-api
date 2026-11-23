using RoomReservations.Application.Abstractions;
using RoomReservations.Application.Exceptions;
using RoomReservations.Application.Reservations;
using RoomReservations.Domain.Entities;
using RoomReservations.Domain.ValueObjects;

namespace RoomBooking.UnitTests.RoomBooking.Application;

public class ReservationServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 24, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task GetForRoomAsync_returns_only_reservations_for_room()
    {
        var room = Room.Create("A", 2);
        var otherRoom = Room.Create("B", 2);
        var first = CreateReservation(room.Id, Now, Now.AddHours(1));
        var other = CreateReservation(otherRoom.Id, Now, Now.AddHours(1));
        var repository = new FakeRepositories([room, otherRoom], [first, other]);

        var result = await Service(repository).GetForRoomAsync(room.Id);

        Assert.Equal(new[] { first.Id }, result.Select(item => item.Id));
        Assert.Null(repository.LastOverlapRange);
    }

    [Fact]
    public async Task GetForRoomAsync_pushes_optional_range_to_repository()
    {
        var room = Room.Create("A", 2);
        var inside = CreateReservation(room.Id, Now, Now.AddHours(1));
        var outside = CreateReservation(room.Id, Now.AddHours(3), Now.AddHours(4));
        var repository = new FakeRepositories([room], [inside, outside]);

        var result = await Service(repository).GetForRoomAsync(room.Id, Now.AddMinutes(30), Now.AddHours(2));

        Assert.Equal(new[] { inside.Id }, result.Select(item => item.Id));
        Assert.Equal(TimeRange.Create(Now.AddMinutes(30), Now.AddHours(2)), repository.LastOverlapRange);
    }

    [Fact]
    public async Task GetForRoomAsync_throws_not_found_for_unknown_room()
    {
        var service = Service(new FakeRepositories([], []));

        await Assert.ThrowsAsync<NotFoundException>(() => service.GetForRoomAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task CreateAsync_creates_reservation()
    {
        var room = Room.Create("A", 2);
        var repository = new FakeRepositories([room], []);

        var result = await Service(repository).CreateAsync(Request(room.Id, Now, Now.AddHours(1)));

        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.Equal(room.Id, result.RoomId);
        Assert.Equal(Now, result.CreatedAt);
        Assert.Single(repository.Items);
        Assert.Equal(1, repository.AddCalls);
    }

    [Fact]
    public async Task CreateAsync_rejects_overlapping_reservation()
    {
        var room = Room.Create("A", 2);
        var existing = CreateReservation(room.Id, Now, Now.AddHours(1));
        var repository = new FakeRepositories([room], [existing]);

        var conflict = await Assert.ThrowsAsync<ConflictException>(() =>
            Service(repository).CreateAsync(Request(room.Id, Now.AddMinutes(30), Now.AddHours(2))));

        Assert.Equal(existing.Id, conflict.ReservationId);
        Assert.Equal(existing.Range.Start, conflict.Start);
        Assert.Equal(existing.Range.End, conflict.End);
        Assert.Equal(1, repository.AddCalls);
        Assert.Single(repository.Items);
    }

    [Fact]
    public async Task CreateAsync_rejects_unknown_room()
    {
        var repository = new FakeRepositories([], []);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            Service(repository).CreateAsync(Request(Guid.NewGuid(), Now, Now.AddHours(1))));

        Assert.Equal(0, repository.AddCalls);
    }

    [Fact]
    public async Task CreateAsync_rejects_invalid_range()
    {
        var room = Room.Create("A", 2);
        var repository = new FakeRepositories([room], []);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            Service(repository).CreateAsync(Request(room.Id, Now, Now)));

        Assert.Equal(0, repository.AddCalls);
    }

    private static ReservationService Service(FakeRepositories repository) =>
        new(repository, repository, new FixedTimeProvider(Now));

    private static CreateReservationRequest Request(Guid roomId, DateTimeOffset start, DateTimeOffset end) =>
        new(roomId, start, end, "Meeting");

    private static Reservation CreateReservation(Guid roomId, DateTimeOffset start, DateTimeOffset end) =>
        Reservation.Create(roomId, TimeRange.Create(start, end), "Meeting", new FixedTimeProvider(Now));

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }

    private sealed class FakeRepositories(Room[] rooms, Reservation[] reservations) : IRoomRepository, IReservationRepository
    {
        public List<Reservation> Items { get; } = [.. reservations];
        public TimeRange? LastOverlapRange { get; private set; }
        public int AddCalls { get; private set; }

        Task<Room?> IRoomRepository.GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
            Task.FromResult(rooms.FirstOrDefault(room => room.Id == id));

        public Task<IReadOnlyList<Room>> GetAllAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Room>>(rooms);

        public Task AddAsync(Room room, CancellationToken cancellationToken = default) => Task.CompletedTask;

        Task<Reservation?> IReservationRepository.GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
            Task.FromResult(Items.FirstOrDefault(reservation => reservation.Id == id));

        public Task<IReadOnlyList<Reservation>> GetByRoomIdAsync(Guid roomId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Reservation>>(Items.Where(reservation => reservation.RoomId == roomId).ToArray());

        public Task<bool> HasOverlappingAsync(Guid roomId, TimeRange range, CancellationToken cancellationToken = default) =>
            Task.FromResult(Items.Any(reservation => reservation.RoomId == roomId && reservation.Range.Overlaps(range)));

        public Task<IReadOnlyList<Reservation>> GetOverlappingAsync(Guid roomId, TimeRange range, CancellationToken cancellationToken = default)
        {
            LastOverlapRange = range;
            return Task.FromResult<IReadOnlyList<Reservation>>(
                Items.Where(reservation => reservation.RoomId == roomId && reservation.Range.Overlaps(range)).ToArray());
        }

        public Task<Reservation?> AddIfNoOverlapAsync(Reservation reservation, CancellationToken cancellationToken = default)
        {
            AddCalls++;
            var conflict = Items.FirstOrDefault(item => item.RoomId == reservation.RoomId && item.Range.Overlaps(reservation.Range));
            if (conflict is not null)
                return Task.FromResult<Reservation?>(conflict);

            Items.Add(reservation);
            return Task.FromResult<Reservation?>(null);
        }
    }
}
