using Microsoft.EntityFrameworkCore;
using TicketingEngine.Domain.Entities;
using TicketingEngine.Domain.ValueObjects;

namespace TicketingEngine.Infrastructure.Persistence;

public static class SeedData
{
    public static async Task SeedAsync(AppDbContext db)
    {
        if (await db.Events.AnyAsync()) return;

        var venue = Venue.Create("Movistar Arena", "Autopista Norte 103-60",
            "Bogotá", 14000);

        var sectionFloor = Section.Create(venue.Id, "Piso General", 5000, 180_000m);
        var sectionVip   = Section.Create(venue.Id, "VIP", 500, 650_000m);

        var ev = Event.Create(venue.Id, "Test Concert — High Load",
            "Stress-test event for concurrency validation",
            DateTime.UtcNow.AddMonths(2));
        ev.Publish();

        var seats = GenerateSeats(sectionFloor.Id, 25, 20)
            .Concat(GenerateSeats(sectionVip.Id, 5, 10));

        var admin = User.Create("admin@ticketing.dev", "Admin User",
            "AQAAAAEAACcQAAAAEPlaceholderHashXXXXXX", UserRole.Admin);

        db.Venues.Add(venue);
        db.Sections.AddRange(sectionFloor, sectionVip);
        db.Events.Add(ev);
        db.Seats.AddRange(seats);
        db.Users.Add(admin);
        await db.SaveChangesAsync();
    }

    private static IEnumerable<Seat> GenerateSeats(
        Guid sectionId, int rows, int seatsPerRow) =>
        Enumerable.Range(0, rows).SelectMany(r =>
            Enumerable.Range(1, seatsPerRow).Select(n =>
                Seat.Create(sectionId, ((char)('A' + r)).ToString(), n)));
}
