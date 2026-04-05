using TicketingEngine.Domain.Entities;

namespace TicketingEngine.Application.Interfaces;

public interface IAppDbContext
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}

public interface ISeatRepository
{
    Task<Seat?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Seat?> GetByIdWithLockAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<Seat>> GetAvailableByEventAsync(Guid eventId, CancellationToken ct = default);
    void Add(Seat seat);
}

public interface IOrderRepository
{
    Task<Order?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Order?> GetByIdWithItemsAsync(Guid id, CancellationToken ct = default);
    Task<Order?> GetByIdempotencyKeyAsync(string key, CancellationToken ct = default);
    Task<IReadOnlyList<Order>> GetExpiredPendingAsync(CancellationToken ct = default);
    void Add(Order order);
}

public interface IReservationRepository
{
    Task<Reservation?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Reservation?> GetBySeatAndOrderAsync(Guid seatId, Guid orderId, CancellationToken ct = default);
    void Add(Reservation reservation);
}

public interface IEventRepository
{
    Task<Event?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<Event>> GetActiveEventsAsync(CancellationToken ct = default);
}

public interface ICacheService
{
    Task<T?> GetAsync<T>(string key, CancellationToken ct = default);
    Task SetAsync<T>(string key, T value, TimeSpan? expiry = null, CancellationToken ct = default);
    Task RemoveAsync(string key, CancellationToken ct = default);
}

public interface IOutboxRepository
{
    void Add(OutboxMessage message);
    Task<IReadOnlyList<OutboxMessage>> GetPendingAsync(int batchSize, CancellationToken ct = default);
}
