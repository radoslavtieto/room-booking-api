using System.Globalization;

namespace RoomReservations.Api.Endpoints;

internal static class DateRangeQueryParser
{
    internal static (DateTimeOffset Start, DateTimeOffset End)? Parse(string? start, string? end)
    {
        if (start is null && end is null)
            return null;

        if (!DateTimeOffset.TryParse(start, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var parsedStart) ||
            !DateTimeOffset.TryParse(end, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var parsedEnd))
            throw new ArgumentException("Both start and end must be valid date-time values.");

        if (parsedEnd <= parsedStart)
            throw new ArgumentException("The end time must be after the start time.");

        return (parsedStart, parsedEnd);
    }
}
