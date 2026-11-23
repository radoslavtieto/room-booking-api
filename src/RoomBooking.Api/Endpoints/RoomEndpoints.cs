using System.Globalization;
using RoomReservations.Application.Rooms;

namespace RoomReservations.Api.Endpoints;

public static class RoomEndpoints
{
    public static IEndpointRouteBuilder MapRoomEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/rooms/available", async (
            string? start,
            string? end,
            RoomService service,
            CancellationToken cancellationToken) =>
        {
            var range = ParseRange(start, end);
            var rooms = await service.GetAvailableAsync(range?.Start, range?.End, cancellationToken);
            return Results.Ok(rooms);
        });

        return endpoints;
    }

    internal static (DateTimeOffset Start, DateTimeOffset End)? ParseRange(string? start, string? end)
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
