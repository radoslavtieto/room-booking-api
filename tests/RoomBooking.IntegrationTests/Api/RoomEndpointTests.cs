using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace RoomBooking.IntegrationTests.Api;

public sealed class RoomEndpointTests
{
    private static readonly DateTimeOffset Start = new(2026, 11, 24, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Get_available_returns_seeded_rooms()
    {
        using var factory = new ApiFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/rooms/available");
        var rooms = await response.Content.ReadFromJsonAsync<JsonElement[]>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(3, rooms!.Length);
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task Get_available_returns_problem_details_for_invalid_range()
    {
        using var factory = new ApiFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync($"/api/rooms/available?{ApiTestHelpers.RangeQuery(Start, Start)}");
        var problem = await ApiTestHelpers.ReadJson(response);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        ApiTestHelpers.AssertProblemDetails(response, problem, 400);
    }

    [Fact]
    public async Task Get_available_with_range_excludes_reserved_room()
    {
        using var factory = new ApiFactory();
        using var client = factory.CreateClient();
        var roomId = await ApiTestHelpers.GetFirstRoomId(client);
        await ApiTestHelpers.PostReservation(client, roomId, Start, Start.AddHours(1));

        var response = await client.GetAsync(
            $"/api/rooms/available?{ApiTestHelpers.RangeQuery(Start, Start.AddHours(1))}");
        var rooms = await response.Content.ReadFromJsonAsync<JsonElement[]>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.DoesNotContain(rooms!, room => room.GetProperty("id").GetGuid() == roomId);
    }
}
