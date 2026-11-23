using RoomReservations.Domain;
using RoomReservations.Domain.Entities;
using RoomReservations.Domain.ValueObjects;

namespace RoomBooking.UnitTests.RoomBooking.Domain;

public class RoomTests
{
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_throws_when_name_is_empty(string name)
    {
        Assert.Throws<DomainException>(() => Room.Create(name, 4));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Create_throws_when_capacity_is_not_positive(int capacity)
    {
        Assert.Throws<DomainException>(() => Room.Create("Conference Room", capacity));
    }

    [Fact]
    public void Create_sets_room_properties()
    {
        var room = Room.Create("Conference Room", 8);

        Assert.NotEqual(Guid.Empty, room.Id);
        Assert.Equal("Conference Room", room.Name);
        Assert.Equal(8, room.Capacity);
    }
}
