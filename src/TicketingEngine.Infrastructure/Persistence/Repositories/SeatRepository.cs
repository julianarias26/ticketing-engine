using Microsoft.EntityFrameworkCore;
using TicketingEngine.Application.Interfaces;
using TicketingEngine.Domain.Entities;
using TicketingEngine.Domain.ValueObjects;

namespace TicketingEngine.Infrastructure.Persistence.Repositories;

public sealed class SeatRepository : ISeatRepository
{
    private readonly AppDbContext _db;
    public SeatRepository(AppDbContext db) => _db = db;

    public Task<Seat?> GetByIdAsync(Guid id, CancellationToken ct) =>
        _db.Seats.Include(s => s.Section).FirstOrDefaultAsync(s => s.Id == id, ct);

    public async Task<Seat?> GetByIdWithLockAsync(Guid id, CancellationToken ct)
    {
        var seat = await _db.Seats
            .FromSqlRaw(
                "SELECT * FROM seats WHERE id = {0} FOR UPDATE SKIP LOCKED", id)
            .AsTracking()
            .FirstOrDefaultAsync(ct);

        if (seat != null)
        {
            await _db.Entry(seat).Reference(s => s.Section).LoadAsync(ct);
        }

        return seat;
    }

    public async Task<IReadOnlyList<Seat>> GetAvailableByEventAsync(
        Guid eventId, CancellationToken ct)
    {
        return await _db.Seats
            .Include(s => s.Section)
            .Where(s => s.Section!.Venue!.Id != Guid.Empty // nav loaded
                && s.Status == SeatStatus.Available
                && _db.Events.Any(e =>
                    e.Id == eventId &&
                    e.VenueId == s.Section!.VenueId))
            .AsNoTracking()
            .ToListAsync(ct);
    }

    public void Add(Seat seat) => _db.Seats.Add(seat);
}
