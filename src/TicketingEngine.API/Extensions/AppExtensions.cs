using Microsoft.EntityFrameworkCore;
using TicketingEngine.Infrastructure.Persistence;

namespace TicketingEngine.API.Extensions;

public static class AppExtensions
{
    public static async Task MigrateAndSeedAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        await db.Database.MigrateAsync();
        await SeedData.SeedAsync(db);
    }
}
