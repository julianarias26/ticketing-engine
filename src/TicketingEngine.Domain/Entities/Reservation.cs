using TicketingEngine.Domain.ValueObjects;

namespace TicketingEngine.Domain.Entities;

public sealed class Reservation : BaseEntity
{
    public Guid SeatId { get; private set; }
    public Guid UserId { get; private set; }
    public Guid OrderId { get; private set; }
    public Guid EventId { get; private set; }
    public ReservationStatus Status { get; private set; }
    public DateTime ExpiresAt { get; private set; }

    private Reservation() { }

    public static Reservation Create(Guid seatId, Guid userId, Guid orderId,
        Guid eventId, DateTime expiresAt) => new()
        {
            SeatId    = seatId,
            UserId    = userId,
            OrderId   = orderId,
            EventId   = eventId,
            Status    = ReservationStatus.Pending,
            ExpiresAt = expiresAt
        };

    public void Confirm() => Status = ReservationStatus.Confirmed;
    public void Expire()  => Status = ReservationStatus.Expired;
    public bool IsExpired() => DateTime.UtcNow > ExpiresAt;
}
