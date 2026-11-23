namespace RoomReservations.Application.Exceptions;

public sealed class ConflictException : Exception
{
    public Guid ReservationId { get; }
    public DateTimeOffset Start { get; }
    public DateTimeOffset End { get; }

    public ConflictException(string message, Guid reservationId, DateTimeOffset start, DateTimeOffset end)
        : base(message)
    {
        ReservationId = reservationId;
        Start = start;
        End = end;
    }
}
