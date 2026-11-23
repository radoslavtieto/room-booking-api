using RoomReservations.Application.Abstractions;
using RoomReservations.Domain.Entities;

namespace RoomReservations.Application.Rooms;

public sealed class RoomService(IRoomRepository rooms, IReservationRepository reservations)
{
    public async Task<IReadOnlyList<RoomResponse>> GetAvailableAsync(
        DateTimeOffset? start = null,
        DateTimeOffset? end = null,
        CancellationToken cancellationToken = default)
    {
        var range = TimeRangeValidator.CreateOptional(start, end);
        var allRooms = await rooms.GetAllAsync(cancellationToken);
        var available = new List<RoomResponse>();

        foreach (var room in allRooms)
        {
            if (range is not null && await reservations.HasOverlappingAsync(room.Id, range.Value, cancellationToken))
                continue;

            available.Add(ToResponse(room));
        }

        return available;
    }

    private static RoomResponse ToResponse(Room room) => new(room.Id, room.Name, room.Capacity);
}
