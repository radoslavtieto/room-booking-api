using RoomReservations.Domain.ValueObjects;

namespace RoomReservations.Domain.Entities;

public sealed class Reservation
{
    public Guid Id { get; private init; }
    public Guid RoomId { get; private init; }
    public DateTimeOffset Start { get; private init; }
    public DateTimeOffset End { get; private init; }
    public TimeRange Range => TimeRange.Create(Start, End);
    public string Title { get; private init; } = string.Empty;
    public DateTimeOffset CreatedAt { get; private init; }

    public TimeRange TimeRange => Range;

    private Reservation()
    {
    }

    private Reservation(Guid id, Guid roomId, TimeRange range, string title, DateTimeOffset createdAt)
    {
        Id = id;
        RoomId = roomId;
        Start = range.Start;
        End = range.End;
        Title = title;
        CreatedAt = createdAt;
    }

    public static Reservation Create(Guid roomId, TimeRange range, string title, TimeProvider timeProvider)
    {       
        if (roomId == Guid.Empty)
            throw new DomainException("A room id is required.");

        ArgumentNullException.ThrowIfNull(timeProvider);

        return new Reservation(Guid.NewGuid(), roomId, range, title, timeProvider.GetUtcNow());
    }
}
