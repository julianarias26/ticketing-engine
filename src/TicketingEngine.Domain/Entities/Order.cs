using TicketingEngine.Domain.Events;
using TicketingEngine.Domain.Exceptions;
using TicketingEngine.Domain.ValueObjects;

namespace TicketingEngine.Domain.Entities;

public sealed class Order : BaseEntity
{
    public Guid UserId { get; private set; }
    public OrderStatus Status { get; private set; }
    public decimal TotalAmount { get; private set; }
    public string IdempotencyKey { get; private set; } = default!;
    public DateTime ExpiresAt { get; private set; }
    public IReadOnlyCollection<OrderItem> Items => _items.AsReadOnly();
    private readonly List<OrderItem> _items = [];

    private Order() { }

    public static Order Create(Guid userId, string idempotencyKey, int expirationMinutes = 15)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(idempotencyKey);
        var order = new Order
        {
            UserId         = userId,
            Status         = OrderStatus.Pending,
            TotalAmount    = 0,
            IdempotencyKey = idempotencyKey,
            ExpiresAt      = DateTime.UtcNow.AddMinutes(expirationMinutes)
        };
        order.AddDomainEvent(new OrderCreatedDomainEvent(order.Id, userId));
        return order;
    }

    public void AddItem(Guid seatId, Guid eventId, decimal unitPrice)
    {
        if (Status != OrderStatus.Pending)
            throw new OrderNotModifiableException(Id, Status);
        _items.Add(new OrderItem(Id, seatId, eventId, unitPrice));
        TotalAmount += unitPrice;
    }

    public void MarkPaid()
    {
        if (Status != OrderStatus.Pending)
            throw new OrderNotModifiableException(Id, Status);
        Status = OrderStatus.Paid;
        AddDomainEvent(new OrderPaidDomainEvent(Id, UserId, TotalAmount));
    }

    public void Expire()
    {
        if (Status != OrderStatus.Pending) return;
        Status = OrderStatus.Expired;
        AddDomainEvent(new OrderExpiredDomainEvent(Id, UserId));
    }

    public bool IsExpired() => DateTime.UtcNow > ExpiresAt;
}
