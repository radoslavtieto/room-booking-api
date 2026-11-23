using Microsoft.EntityFrameworkCore;
using RoomReservations.Api;
using RoomReservations.Api.Endpoints;
using RoomReservations.Application.Abstractions;
using RoomReservations.Application.Reservations;
using RoomReservations.Application.Rooms;
using RoomReservations.Infrastructure.Persistence;
using RoomReservations.Infrastructure.Persistence.Repositories;
using RoomReservations.Infrastructure.Persistence.Seed;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ProblemDetailsExceptionHandler>();
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddScoped<IRoomRepository, RoomRepository>();
builder.Services.AddScoped<IReservationRepository, ReservationRepository>();
builder.Services.AddScoped<RoomService>();
builder.Services.AddScoped<ReservationService>();
builder.Services.AddSingleton(TimeProvider.System);

var app = builder.Build();
app.UseExceptionHandler();
app.MapRoomEndpoints();
app.MapReservationEndpoints();

await using (var scope = app.Services.CreateAsyncScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await dbContext.Database.MigrateAsync();
    await RoomSeeder.SeedAsync(dbContext);
}

app.Run();

public partial class Program { }
