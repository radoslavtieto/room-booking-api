using RoomReservations.Application.Reservations;

namespace RoomReservations.Api.Endpoints;

public static class ReservationEndpoints
{
    public static IEndpointRouteBuilder MapReservationEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/rooms/{roomId:guid}/reservations");

        group.MapGet("", async (
            Guid roomId,
            string? start,
            string? end,
            ReservationService service,
            CancellationToken cancellationToken) =>
        {
            var range = RoomEndpoints.ParseRange(start, end);
            var reservations = await service.GetForRoomAsync(roomId, range?.Start, range?.End, cancellationToken);
            return Results.Ok(reservations);
        });

        group.MapPost("", async (
            Guid roomId,
            CreateReservationBody body,
            ReservationService service,
            CancellationToken cancellationToken) =>
        {
            var created = await service.CreateAsync(
                new CreateReservationRequest(roomId, body.Start, body.End, body.Title),
                cancellationToken);
            return Results.Created($"/api/rooms/{roomId}/reservations/{created.Id}", created);
        });

        return endpoints;
    }

    public sealed record CreateReservationBody(DateTimeOffset Start, DateTimeOffset End, string Title);
}
