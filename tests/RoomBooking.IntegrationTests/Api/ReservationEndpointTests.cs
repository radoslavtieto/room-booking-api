using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace RoomBooking.IntegrationTests.Api;

public sealed class ReservationEndpointTests
{
    private static readonly DateTimeOffset Start = new(2026, 11, 24, 12, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset TenAm = new(2026, 11, 24, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Get_room_reservations_returns_404_problem_details_for_unknown_room()
    {
        using var factory = new ApiFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync($"/api/rooms/{Guid.NewGuid()}/reservations");
        var problem = await ApiTestHelpers.ReadJson(response);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        ApiTestHelpers.AssertProblemDetails(response, problem, 404);
    }

    [Fact]
    public async Task Get_room_reservations_filters_by_optional_range()
    {
        using var factory = new ApiFactory();
        using var client = factory.CreateClient();
        var roomId = await ApiTestHelpers.GetFirstRoomId(client);
        await ApiTestHelpers.PostReservation(client, roomId, Start, Start.AddHours(1));
        await ApiTestHelpers.PostReservation(client, roomId, Start.AddHours(3), Start.AddHours(4));

        var response = await client.GetAsync(
            $"/api/rooms/{roomId}/reservations?{ApiTestHelpers.RangeQuery(Start, Start.AddHours(2))}");
        var reservations = await response.Content.ReadFromJsonAsync<JsonElement[]>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Single(reservations!);
        Assert.Equal(Start, reservations![0].GetProperty("start").GetDateTimeOffset());
    }

    [Fact]
    public async Task Post_reservation_returns_404_problem_details_for_unknown_room()
    {
        using var factory = new ApiFactory();
        using var client = factory.CreateClient();

        var response = await ApiTestHelpers.PostReservation(client, Guid.NewGuid(), Start, Start.AddHours(1));
        var problem = await ApiTestHelpers.ReadJson(response);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        ApiTestHelpers.AssertProblemDetails(response, problem, 404);
    }

    [Fact]
    public async Task Post_reservation_returns_created_body_and_get_lists_it()
    {
        using var factory = new ApiFactory();
        using var client = factory.CreateClient();
        var roomId = await ApiTestHelpers.GetFirstRoomId(client);

        var response = await ApiTestHelpers.PostReservation(client, roomId, Start, Start.AddHours(1));
        var created = await ApiTestHelpers.ReadJson(response);
        var listResponse = await client.GetAsync($"/api/rooms/{roomId}/reservations");
        var list = await listResponse.Content.ReadFromJsonAsync<JsonElement[]>();

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal(roomId, created.GetProperty("roomId").GetGuid());
        Assert.Equal("Meeting", created.GetProperty("title").GetString());
        Assert.NotEqual(Guid.Empty, created.GetProperty("id").GetGuid());
        Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);
        Assert.Single(list!);
    }

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

    [Fact]
    public async Task Each_api_factory_uses_a_fresh_database()
    {
        Guid roomId;
        using (var firstFactory = new ApiFactory())
        using (var firstClient = firstFactory.CreateClient())
        {
            roomId = await ApiTestHelpers.GetFirstRoomId(firstClient);
            var created = await ApiTestHelpers.PostReservation(firstClient, roomId, TenAm, TenAm.AddHours(1));
            Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        }

        using var secondFactory = new ApiFactory();
        using var secondClient = secondFactory.CreateClient();
        var response = await secondClient.GetAsync($"/api/rooms/{roomId}/reservations");
        var problem = await ApiTestHelpers.ReadJson(response);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        ApiTestHelpers.AssertProblemDetails(response, problem, 404);
    }

    [Fact]
    public async Task Post_reservation_returns_400_problem_details_for_invalid_range()
    {
        using var factory = new ApiFactory();
        using var client = factory.CreateClient();
        var roomId = await ApiTestHelpers.GetFirstRoomId(client);

        var response = await ApiTestHelpers.PostReservation(client, roomId, Start, Start);
        var problem = await ApiTestHelpers.ReadJson(response);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        ApiTestHelpers.AssertProblemDetails(response, problem, 400);
    }
}
