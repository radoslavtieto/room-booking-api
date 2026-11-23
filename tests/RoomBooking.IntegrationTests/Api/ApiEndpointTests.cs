using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using RoomReservations.Api;
using RoomReservations.Infrastructure.Persistence;

namespace RoomBooking.IntegrationTests.Api;

public sealed class ApiEndpointTests
{
    private static readonly DateTimeOffset Start = new(2026, 11, 24, 12, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset TenAm = new(2026, 11, 24, 10, 0, 0, TimeSpan.Zero);

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

        var response = await client.GetAsync($"/api/rooms/available?{RangeQuery(Start, Start)}");
        var problem = await ReadJson(response);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        AssertProblemDetails(response, problem, 400);
    }

    [Fact]
    public async Task Get_room_reservations_returns_404_problem_details_for_unknown_room()
    {
        using var factory = new ApiFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync($"/api/rooms/{Guid.NewGuid()}/reservations");
        var problem = await ReadJson(response);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        AssertProblemDetails(response, problem, 404);
    }

    [Fact]
    public async Task Get_room_reservations_filters_by_optional_range()
    {
        using var factory = new ApiFactory();
        using var client = factory.CreateClient();
        var roomId = await GetFirstRoomId(client);
        await PostReservation(client, roomId, Start, Start.AddHours(1));
        await PostReservation(client, roomId, Start.AddHours(3), Start.AddHours(4));

        var response = await client.GetAsync(
            $"/api/rooms/{roomId}/reservations?{RangeQuery(Start, Start.AddHours(2))}");
        var reservations = await response.Content.ReadFromJsonAsync<JsonElement[]>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Single(reservations!);
        Assert.Equal(Start, reservations![0].GetProperty("start").GetDateTimeOffset());
    }

    [Fact]
    public async Task Get_available_with_range_excludes_reserved_room()
    {
        using var factory = new ApiFactory();
        using var client = factory.CreateClient();
        var roomId = await GetFirstRoomId(client);
        await PostReservation(client, roomId, Start, Start.AddHours(1));

        var response = await client.GetAsync(
            $"/api/rooms/available?{RangeQuery(Start, Start.AddHours(1))}");
        var rooms = await response.Content.ReadFromJsonAsync<JsonElement[]>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.DoesNotContain(rooms!, room => room.GetProperty("id").GetGuid() == roomId);
    }

    [Fact]
    public async Task Post_reservation_returns_404_problem_details_for_unknown_room()
    {
        using var factory = new ApiFactory();
        using var client = factory.CreateClient();

        var response = await PostReservation(client, Guid.NewGuid(), Start, Start.AddHours(1));
        var problem = await ReadJson(response);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        AssertProblemDetails(response, problem, 404);
    }

    [Fact]
    public async Task Post_reservation_returns_created_body_and_get_lists_it()
    {
        using var factory = new ApiFactory();
        using var client = factory.CreateClient();
        var roomId = await GetFirstRoomId(client);

        var response = await PostReservation(client, roomId, Start, Start.AddHours(1));
        var created = await ReadJson(response);
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
    public async Task Post_adjacent_bookings_both_succeed()
    {
        using var factory = new ApiFactory();
        using var client = factory.CreateClient();
        var roomId = await GetFirstRoomId(client);

        var first = await PostReservation(client, roomId, TenAm, TenAm.AddHours(1));
        var next = await PostReservation(client, roomId, TenAm.AddHours(1), TenAm.AddHours(2));

        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        Assert.Equal(HttpStatusCode.Created, next.StatusCode);
    }

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
        var roomId = await GetFirstRoomId(client);
        var first = await PostReservation(client, roomId, TenAm, TenAm.AddHours(2));

        var conflict = await PostReservation(
            client,
            roomId,
            new DateTimeOffset(2026, 11, 24, incomingStartHour, incomingStartMinute, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 11, 24, incomingEndHour, incomingEndMinute, 0, TimeSpan.Zero));

        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, conflict.StatusCode);
    }

    [Fact]
    public async Task Post_same_slot_in_different_rooms_succeeds()
    {
        using var factory = new ApiFactory();
        using var client = factory.CreateClient();
        var rooms = await client.GetFromJsonAsync<JsonElement[]>("/api/rooms/available");
        var firstRoomId = rooms![0].GetProperty("id").GetGuid();
        var secondRoomId = rooms[1].GetProperty("id").GetGuid();

        var first = await PostReservation(client, firstRoomId, TenAm, TenAm.AddHours(1));
        var second = await PostReservation(client, secondRoomId, TenAm, TenAm.AddHours(1));

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
            roomId = await GetFirstRoomId(firstClient);
            var created = await PostReservation(firstClient, roomId, TenAm, TenAm.AddHours(1));
            Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        }

        using var secondFactory = new ApiFactory();
        using var secondClient = secondFactory.CreateClient();
        var response = await secondClient.GetAsync($"/api/rooms/{roomId}/reservations");
        var problem = await ReadJson(response);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        AssertProblemDetails(response, problem, 404);
    }

    [Fact]
    public async Task Post_reservation_returns_409_problem_details_with_conflicting_reservation_range()
    {
        using var factory = new ApiFactory();
        using var client = factory.CreateClient();
        var roomId = await GetFirstRoomId(client);
        var firstResponse = await PostReservation(client, roomId, Start, Start.AddHours(1));
        var first = await ReadJson(firstResponse);

        var conflictResponse = await PostReservation(client, roomId, Start.AddMinutes(30), Start.AddHours(2));
        var problem = await ReadJson(conflictResponse);

        Assert.Equal(HttpStatusCode.Conflict, conflictResponse.StatusCode);
        AssertProblemDetails(conflictResponse, problem, 409);
        Assert.Equal(first.GetProperty("id").GetGuid(), problem.GetProperty("reservationId").GetGuid());
        Assert.Equal(Start, problem.GetProperty("start").GetDateTimeOffset());
        Assert.Equal(Start.AddHours(1), problem.GetProperty("end").GetDateTimeOffset());
    }

    [Fact]
    public async Task Post_reservation_returns_400_problem_details_for_invalid_range()
    {
        using var factory = new ApiFactory();
        using var client = factory.CreateClient();
        var roomId = await GetFirstRoomId(client);

        var response = await PostReservation(client, roomId, Start, Start);
        var problem = await ReadJson(response);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        AssertProblemDetails(response, problem, 400);
    }

    private static string RangeQuery(DateTimeOffset start, DateTimeOffset end) =>
        $"start={Uri.EscapeDataString(start.ToString("O"))}&end={Uri.EscapeDataString(end.ToString("O"))}";

    private static async Task<Guid> GetFirstRoomId(HttpClient client)
    {
        var rooms = await client.GetFromJsonAsync<JsonElement[]>("/api/rooms/available");
        return rooms![0].GetProperty("id").GetGuid();
    }

    private static Task<HttpResponseMessage> PostReservation(
        HttpClient client,
        Guid roomId,
        DateTimeOffset start,
        DateTimeOffset end) =>
        client.PostAsJsonAsync($"/api/rooms/{roomId}/reservations", new
        {
            start,
            end,
            title = "Meeting"
        });

    private static async Task<JsonElement> ReadJson(HttpResponseMessage response) =>
        await response.Content.ReadFromJsonAsync<JsonElement>();

    private static void AssertProblemDetails(HttpResponseMessage response, JsonElement problem, int status)
    {
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        Assert.Equal(status, problem.GetProperty("status").GetInt32());
        Assert.True(problem.TryGetProperty("title", out _));
        Assert.True(problem.TryGetProperty("detail", out _));
    }

    private sealed class ApiFactory : WebApplicationFactory<Program>
    {
        private readonly SqliteConnection connection = new("Data Source=:memory:");

        public ApiFactory() => connection.Open();

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<AppDbContext>();
                services.RemoveAll<DbContextOptions<AppDbContext>>();
                services.AddDbContext<AppDbContext>(options => options.UseSqlite(connection));
            });
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (disposing)
                connection.Dispose();
        }
    }
}
