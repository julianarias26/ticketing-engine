namespace TicketingEngine.Domain.Entities;

public sealed class OrderItem
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid OrderId { get; private set; }
    public Guid SeatId { get; private set; }
    public Guid EventId { get; private set; }
    public decimal UnitPrice { get; private set; }
    public Seat? Seat { get; private set; }

    private OrderItem() { }

    public OrderItem(Guid orderId, Guid seatId, Guid eventId, decimal unitPrice)
    {
        if (unitPrice < 0) throw new ArgumentOutOfRangeException(nameof(unitPrice));
        OrderId   = orderId;
        SeatId    = seatId;
        EventId   = eventId;
        UnitPrice = unitPrice;
    }
}
