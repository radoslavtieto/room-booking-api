using Microsoft.EntityFrameworkCore;
using RoomReservations.Infrastructure.Persistence;
using RoomReservations.Infrastructure.Persistence.Seed;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

var app = builder.Build();

await using (var scope = app.Services.CreateAsyncScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await dbContext.Database.MigrateAsync();
    await RoomSeeder.SeedAsync(dbContext);
}

app.Run();
