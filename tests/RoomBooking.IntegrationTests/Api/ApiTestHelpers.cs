using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using RoomReservations.Infrastructure.Persistence;

namespace RoomBooking.IntegrationTests.Api;

internal sealed class ApiFactory : WebApplicationFactory<Program>
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

internal static class ApiTestHelpers
{
    public static string RangeQuery(DateTimeOffset start, DateTimeOffset end) =>
        $"start={Uri.EscapeDataString(start.ToString("O"))}&end={Uri.EscapeDataString(end.ToString("O"))}";

    public static async Task<Guid> GetFirstRoomId(HttpClient client)
    {
        var rooms = await client.GetFromJsonAsync<JsonElement[]>("/api/rooms/available");
        return rooms![0].GetProperty("id").GetGuid();
    }

    public static Task<HttpResponseMessage> PostReservation(
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

    public static async Task<JsonElement> ReadJson(HttpResponseMessage response) =>
        await response.Content.ReadFromJsonAsync<JsonElement>();

    public static void AssertProblemDetails(HttpResponseMessage response, JsonElement problem, int status)
    {
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        Assert.Equal(status, problem.GetProperty("status").GetInt32());
        Assert.True(problem.TryGetProperty("title", out _));
        Assert.True(problem.TryGetProperty("detail", out _));
    }
}
