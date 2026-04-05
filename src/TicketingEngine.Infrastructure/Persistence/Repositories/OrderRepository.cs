using Microsoft.EntityFrameworkCore;
using TicketingEngine.Application.Interfaces;
using TicketingEngine.Domain.Entities;
using TicketingEngine.Domain.ValueObjects;

namespace TicketingEngine.Infrastructure.Persistence.Repositories;

public sealed class OrderRepository : IOrderRepository
{
    private readonly AppDbContext _db;
    public OrderRepository(AppDbContext db) => _db = db;

    public Task<Order?> GetByIdAsync(Guid id, CancellationToken ct) =>
        _db.Orders.FirstOrDefaultAsync(o => o.Id == id, ct);

    public Task<Order?> GetByIdWithItemsAsync(Guid id, CancellationToken ct) =>
        _db.Orders.Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == id, ct);

    public Task<Order?> GetByIdempotencyKeyAsync(string key, CancellationToken ct) =>
        _db.Orders.Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.IdempotencyKey == key, ct);

    public async Task<IReadOnlyList<Order>> GetExpiredPendingAsync(CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        return await _db.Orders.Include(o => o.Items)
            .Where(o => o.Status == OrderStatus.Pending && o.ExpiresAt < now)
            .ToListAsync(ct);
    }

    public void Add(Order order) => _db.Orders.Add(order);
}
