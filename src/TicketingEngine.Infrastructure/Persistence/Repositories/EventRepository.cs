using Microsoft.EntityFrameworkCore;
using TicketingEngine.Application.Interfaces;
using TicketingEngine.Domain.Entities;
using TicketingEngine.Domain.ValueObjects;

namespace TicketingEngine.Infrastructure.Persistence.Repositories;

public sealed class EventRepository : IEventRepository
{
    private readonly AppDbContext _db;
    public EventRepository(AppDbContext db) => _db = db;

    public Task<Event?> GetByIdAsync(Guid id, CancellationToken ct) =>
        _db.Events.Include(e => e.Venue)
            .FirstOrDefaultAsync(e => e.Id == id, ct);

    public async Task<IReadOnlyList<Event>> GetActiveEventsAsync(CancellationToken ct)
    {
        return await _db.Events.Include(e => e.Venue)
            .Where(e => e.Status == EventStatus.OnSale
                     || e.Status == EventStatus.Published)
            .OrderBy(e => e.EventDate)
            .AsNoTracking()
            .ToListAsync(ct);
    }
}
