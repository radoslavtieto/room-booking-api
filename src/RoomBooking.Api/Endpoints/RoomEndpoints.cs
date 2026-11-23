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
            var range = DateRangeQueryParser.Parse(start, end);
            var rooms = await service.GetAvailableAsync(range?.Start, range?.End, cancellationToken);
            return Results.Ok(rooms);
        });

        return endpoints;
    }
}
