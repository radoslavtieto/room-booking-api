using RoomReservations.Domain;
using RoomReservations.Domain.Entities;
using RoomReservations.Domain.ValueObjects;

namespace RoomBooking.UnitTests.RoomBooking.Domain;

public class ReservationTests
{
    private static readonly DateTimeOffset Now =
        new(2026, 11, 24, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Create_throws_when_range_is_invalid()
    {
        Assert.Throws<DomainException>(() =>
            TimeRange.Create(Now, Now));
    }

    [Fact]
    public void Create_uses_time_provider_for_created_at()
    {
        var roomId = Guid.NewGuid();
        var range = TimeRange.Create(Now.AddHours(1), Now.AddHours(2));

        var reservation = Reservation.Create(
            roomId,
            range,
            "Team meeting",
            new FixedTimeProvider(Now));

        Assert.NotEqual(Guid.Empty, reservation.Id);
        Assert.Equal(roomId, reservation.RoomId);
        Assert.Equal(range, reservation.Range);
        Assert.Equal("Team meeting", reservation.Title);
        Assert.Equal(Now, reservation.CreatedAt);
    }

    private sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }
}
