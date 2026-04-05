using Microsoft.EntityFrameworkCore;
using TicketingEngine.Application.Interfaces;
using TicketingEngine.Domain.Entities;

namespace TicketingEngine.Infrastructure.Persistence.Repositories;

public sealed class OutboxRepository : IOutboxRepository
{
    private readonly AppDbContext _db;
    public OutboxRepository(AppDbContext db) => _db = db;

    public void Add(OutboxMessage message) => _db.OutboxMessages.Add(message);

    public async Task<IReadOnlyList<OutboxMessage>> GetPendingAsync(
        int batchSize, CancellationToken ct)
    {
        return await _db.OutboxMessages
            .Where(m => m.ProcessedAt == null && m.RetryCount < 5)
            .OrderBy(m => m.CreatedAt)
            .Take(batchSize)
            .ToListAsync(ct);
    }
}
