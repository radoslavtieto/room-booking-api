using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace RoomBooking.IntegrationTests.Api;

public sealed class ReservationEndpointConflictTests
{
    private static readonly DateTimeOffset Start = new(2026, 11, 24, 12, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset TenAm = new(2026, 11, 24, 10, 0, 0, TimeSpan.Zero);

    // In one room, 10:00-11:00 and 11:00-12:00 are back-to-back and both should succeed.
    [Fact]
    public async Task Post_adjacent_bookings_both_succeed()
    {
        using var factory = new ApiFactory();
        using var client = factory.CreateClient();
        var roomId = await ApiTestHelpers.GetFirstRoomId(client);

        var first = await ApiTestHelpers.PostReservation(client, roomId, TenAm, TenAm.AddHours(1));
        var next = await ApiTestHelpers.PostReservation(client, roomId, TenAm.AddHours(1), TenAm.AddHours(2));

        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        Assert.Equal(HttpStatusCode.Created, next.StatusCode);
    }

    // In one room, 09:00-10:00 may end exactly as an existing 10:00-11:00 booking starts.
    [Fact]
    public async Task Post_booking_ending_at_existing_start_succeeds()
    {
        using var factory = new ApiFactory();
        using var client = factory.CreateClient();
        var roomId = await ApiTestHelpers.GetFirstRoomId(client);
        var existing = await ApiTestHelpers.PostReservation(client, roomId, TenAm, TenAm.AddHours(1));

        var adjacent = await ApiTestHelpers.PostReservation(client, roomId, TenAm.AddHours(-1), TenAm);
        var listResponse = await client.GetAsync($"/api/rooms/{roomId}/reservations");
        var reservations = await listResponse.Content.ReadFromJsonAsync<JsonElement[]>();

        Assert.Equal(HttpStatusCode.Created, existing.StatusCode);
        Assert.Equal(HttpStatusCode.Created, adjacent.StatusCode);
        Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);
        Assert.Equal(2, reservations!.Length);
    }

    // With an existing 10:00-11:00 booking, 09:00-10:00:00.0000001 and 10:59:59.9999999-12:00 each overlap by one tick and must conflict.
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Post_booking_overlapping_by_one_tick_returns_conflict(bool overlapsAtStart)
    {
        using var factory = new ApiFactory();
        using var client = factory.CreateClient();
        var roomId = await ApiTestHelpers.GetFirstRoomId(client);
        var existing = await ApiTestHelpers.PostReservation(client, roomId, TenAm, TenAm.AddHours(1));
        var incomingStart = overlapsAtStart ? TenAm.AddHours(-1) : TenAm.AddHours(1).AddTicks(-1);
        var incomingEnd = overlapsAtStart ? TenAm.AddTicks(1) : TenAm.AddHours(2);

        var conflict = await ApiTestHelpers.PostReservation(client, roomId, incomingStart, incomingEnd);
        var listResponse = await client.GetAsync($"/api/rooms/{roomId}/reservations");
        var reservations = await listResponse.Content.ReadFromJsonAsync<JsonElement[]>();

        Assert.Equal(HttpStatusCode.Created, existing.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, conflict.StatusCode);
        Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);
        Assert.Single(reservations!);
    }

    // Against an existing 10:00-12:00 booking, 09:30-10:30 (partial), 10:15-11:45 (contained), and 10:00-12:00 (same slot) must conflict.
    [Theory]
    [InlineData(9, 30, 10, 30)]
    [InlineData(10, 15, 11, 45)]
    [InlineData(10, 0, 12, 0)]
    public async Task Post_overlapping_slots_returns_conflict(
        int incomingStartHour,
        int incomingStartMinute,
        int incomingEndHour,
        int incomingEndMinute)
    {
        using var factory = new ApiFactory();
        using var client = factory.CreateClient();
        var roomId = await ApiTestHelpers.GetFirstRoomId(client);
        var first = await ApiTestHelpers.PostReservation(client, roomId, TenAm, TenAm.AddHours(2));

        var conflict = await ApiTestHelpers.PostReservation(
            client,
            roomId,
            new DateTimeOffset(2026, 11, 24, incomingStartHour, incomingStartMinute, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 11, 24, incomingEndHour, incomingEndMinute, 0, TimeSpan.Zero));

        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, conflict.StatusCode);
    }

    // The same 10:00-11:00 slot may be booked in two different rooms.
    [Fact]
    public async Task Post_same_slot_in_different_rooms_succeeds()
    {
        using var factory = new ApiFactory();
        using var client = factory.CreateClient();
        var rooms = await client.GetFromJsonAsync<JsonElement[]>("/api/rooms/available");
        var firstRoomId = rooms![0].GetProperty("id").GetGuid();
        var secondRoomId = rooms[1].GetProperty("id").GetGuid();

        var first = await ApiTestHelpers.PostReservation(client, firstRoomId, TenAm, TenAm.AddHours(1));
        var second = await ApiTestHelpers.PostReservation(client, secondRoomId, TenAm, TenAm.AddHours(1));

        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        Assert.Equal(HttpStatusCode.Created, second.StatusCode);
    }

    // With 12:00-13:00 already booked, a 12:30-14:00 request must report the existing 12:00-13:00 reservation.
    [Fact]
    public async Task Post_reservation_returns_409_problem_details_with_conflicting_reservation_range()
    {
        using var factory = new ApiFactory();
        using var client = factory.CreateClient();
        var roomId = await ApiTestHelpers.GetFirstRoomId(client);
        var firstResponse = await ApiTestHelpers.PostReservation(client, roomId, Start, Start.AddHours(1));
        var first = await ApiTestHelpers.ReadJson(firstResponse);

        var conflictResponse = await ApiTestHelpers.PostReservation(client, roomId, Start.AddMinutes(30), Start.AddHours(2));
        var problem = await ApiTestHelpers.ReadJson(conflictResponse);

        Assert.Equal(HttpStatusCode.Conflict, conflictResponse.StatusCode);
        ApiTestHelpers.AssertProblemDetails(conflictResponse, problem, 409);
        Assert.Equal(first.GetProperty("id").GetGuid(), problem.GetProperty("reservationId").GetGuid());
        Assert.Equal(Start, problem.GetProperty("start").GetDateTimeOffset());
        Assert.Equal(Start.AddHours(1), problem.GetProperty("end").GetDateTimeOffset());
    }
}
