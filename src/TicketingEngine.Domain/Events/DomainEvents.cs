namespace TicketingEngine.Domain.Events;

// Domain events (in-process)
public sealed record SeatReservedDomainEvent(Guid SeatId, Guid SectionId);
public sealed record SeatReleasedDomainEvent(Guid SeatId, Guid SectionId);
public sealed record OrderCreatedDomainEvent(Guid OrderId, Guid UserId);
public sealed record OrderPaidDomainEvent(Guid OrderId, Guid UserId, decimal Amount);
public sealed record OrderExpiredDomainEvent(Guid OrderId, Guid UserId);

// Integration events (published via Outbox → RabbitMQ)
public sealed record SeatReservedEvent(Guid SeatId, Guid EventId, Guid UserId, Guid OrderId, DateTime ExpiresAt);
public sealed record SeatReleasedEvent(Guid SeatId, Guid EventId, Guid OrderId);
public sealed record PaymentProcessedEvent(Guid OrderId, Guid UserId, decimal Amount, string Provider);
public sealed record OrderExpiredEvent(Guid OrderId, Guid UserId, IReadOnlyCollection<Guid> SeatIds);
