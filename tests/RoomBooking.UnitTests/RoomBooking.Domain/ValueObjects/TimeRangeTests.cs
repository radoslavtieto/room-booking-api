using RoomReservations.Domain;
using RoomReservations.Domain.ValueObjects;

namespace RoomBooking.UnitTests.RoomBooking.Domain.ValueObjects;

public class TimeRangeTests
{
    private static readonly DateTimeOffset Day =
        new(2026, 11, 24, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Create_throws_when_end_equals_start()
    {
        var time = Day.AddHours(10);

        Assert.Throws<DomainException>(() => TimeRange.Create(time, time));
    }

    [Fact]
    public void Create_throws_when_end_is_before_start()
    {
        Assert.Throws<DomainException>(() =>
            TimeRange.Create(Day.AddHours(11), Day.AddHours(10)));
    }

    [Fact]
    public void Create_normalizes_both_bounds_to_utc()
    {
        var plusTwo = new DateTimeOffset(2026, 11, 24, 12, 0, 0, TimeSpan.FromHours(2));
        var range = TimeRange.Create(plusTwo, plusTwo.AddHours(1));

        Assert.Equal(TimeSpan.Zero, range.Start.Offset);
        Assert.Equal(new DateTimeOffset(2026, 11, 24, 10, 0, 0, TimeSpan.Zero), range.Start);
        Assert.Equal(new DateTimeOffset(2026, 11, 24, 11, 0, 0, TimeSpan.Zero), range.End);
    }

    [Fact]
    public void Create_sets_duration_to_end_minus_start()
    {
        var range = TimeRange.Create(Day.AddHours(10), Day.AddHours(12));

        Assert.Equal(TimeSpan.FromHours(2), range.Duration);
    }

    [Fact]
    public void Overlaps_returns_true_for_ranges_with_a_shared_instant()
    {
        var first = TimeRange.Create(Day.AddHours(10), Day.AddHours(11));
        var second = TimeRange.Create(Day.AddHours(10).AddMinutes(30), Day.AddHours(12));

        Assert.True(first.Overlaps(second));
        Assert.True(second.Overlaps(first));
    }

    [Fact]
    public void Overlaps_returns_false_for_adjacent_ranges()
    {
        var first = TimeRange.Create(Day.AddHours(10), Day.AddHours(11));
        var second = TimeRange.Create(Day.AddHours(11), Day.AddHours(12));

        Assert.False(first.Overlaps(second));
        Assert.False(second.Overlaps(first));
    }

    [Fact]
    public void Contains_includes_start_and_excludes_end()
    {
        var range = TimeRange.Create(Day.AddHours(10), Day.AddHours(11));

        Assert.True(range.Contains(Day.AddHours(10)));
        Assert.True(range.Contains(Day.AddHours(10).AddMinutes(30)));
        Assert.False(range.Contains(Day.AddHours(11)));
    }

    [Fact]
    public void Two_ranges_with_the_same_instants_are_equal()
    {
        var a = TimeRange.Create(Day.AddHours(10), Day.AddHours(11));
        var b = TimeRange.Create(Day.AddHours(10), Day.AddHours(11));

        Assert.Equal(a, b);
    }
}
