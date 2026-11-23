using RoomReservations.Domain.ValueObjects;

namespace RoomReservations.Application.Abstractions;

internal static class TimeRangeValidator
{
    public static TimeRange Create(DateTimeOffset start, DateTimeOffset end)
    {
        if (end <= start)
            throw new ArgumentException("The end time must be after the start time.");

        return TimeRange.Create(start, end);
    }

    public static TimeRange? CreateOptional(DateTimeOffset? start, DateTimeOffset? end)
    {
        if (start is null && end is null)
            return null;

        if (start is null || end is null)
            throw new ArgumentException("Both start and end times are required.");

        return Create(start.Value, end.Value);
    }
}
