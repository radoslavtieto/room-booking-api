namespace RoomReservations.Domain.Entities;

public sealed class Room
{
    public Guid Id { get; private init; }
    public string Name { get; private init; } = string.Empty;
    public int Capacity { get; private init; }

    private Room()
    {
    }

    private Room(Guid id, string name, int capacity)
    {
        Id = id;
        Name = name;
        Capacity = capacity;
    }

    public static Room Create(string name, int capacity) =>
        Create(Guid.NewGuid(), name, capacity);

    public static Room Create(Guid id, string name, int capacity)
    {
        if (id == Guid.Empty)
            throw new DomainException("A room id is required.");

        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("A room name is required.");

        if (capacity <= 0)
            throw new DomainException("A room capacity must be positive.");

        return new Room(id, name.Trim(), capacity);
    }
}
