using TicketingEngine.Domain.ValueObjects;

namespace TicketingEngine.Domain.Exceptions;

public abstract class DomainException(string message) : Exception(message);

public sealed class SeatNotFoundException(Guid seatId)
    : DomainException($"Seat '{seatId}' was not found.");

public sealed class SeatAlreadyReservedException(Guid seatId)
    : DomainException($"Seat '{seatId}' is already reserved.");

public sealed class InvalidSeatStatusTransitionException(Guid id, SeatStatus from, SeatStatus to)
    : DomainException($"Cannot transition seat '{id}' from {from} to {to}.");

public sealed class OrderNotFoundException(Guid orderId)
    : DomainException($"Order '{orderId}' was not found.");

public sealed class OrderNotModifiableException(Guid orderId, OrderStatus status)
    : DomainException($"Order '{orderId}' cannot be modified in status '{status}'.");

public sealed class EventNotFoundException(Guid eventId)
    : DomainException($"Event '{eventId}' was not found.");
