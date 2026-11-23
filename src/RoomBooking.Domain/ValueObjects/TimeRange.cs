namespace RoomReservations.Domain.ValueObjects;

public readonly record struct TimeRange
{
    public DateTimeOffset Start { get; }
    public DateTimeOffset End { get; }

    private TimeRange(DateTimeOffset start, DateTimeOffset end)
    {
        Start = start;
        End = end;
    }

    /// <summary>
    /// Creates a range in half-open form [Start, End). Both bounds are normalized to UTC
    /// so that comparisons and database storage behave consistently.
    /// </summary>
    public static TimeRange Create(DateTimeOffset start, DateTimeOffset end)
    {
        var utcStart = start.ToUniversalTime();
        var utcEnd = end.ToUniversalTime();

        if (utcEnd <= utcStart)
            throw new DomainException("The end of a time range must be after its start.");

        return new TimeRange(utcStart, utcEnd);
    }

    public TimeSpan Duration => End - Start;

    /// <summary>
    /// True when the two ranges share at least one instant.
    /// Adjacent ranges (one ends exactly where the other starts) do not overlap.
    /// </summary>
    public bool Overlaps(TimeRange other) =>
        Start < other.End && End > other.Start;

    /// <summary>True when the instant falls inside the range. The end is excluded.</summary>
    public bool Contains(DateTimeOffset instant) =>
        instant >= Start && instant < End;
}
