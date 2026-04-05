using FluentAssertions;
using TicketingEngine.Domain.Entities;
using TicketingEngine.Domain.Exceptions;
using TicketingEngine.Domain.ValueObjects;
using Xunit;

namespace TicketingEngine.UnitTests.Domain;

public sealed class OrderTests
{
    [Fact]
    public void Create_ShouldSetPendingStatus_AndRaiseDomainEvent()
    {
        var order = Order.Create(Guid.NewGuid(), "key-123");

        order.Status.Should().Be(OrderStatus.Pending);
        order.TotalAmount.Should().Be(0);
        order.DomainEvents.Should().ContainSingle();
        order.ExpiresAt.Should().BeAfter(DateTime.UtcNow);
    }

    [Fact]
    public void AddItem_ShouldAccumulateTotalAmount()
    {
        var order = Order.Create(Guid.NewGuid(), "key-add");
        order.AddItem(Guid.NewGuid(), Guid.NewGuid(), 100m);
        order.AddItem(Guid.NewGuid(), Guid.NewGuid(), 200m);

        order.TotalAmount.Should().Be(300m);
        order.Items.Should().HaveCount(2);
    }

    [Fact]
    public void MarkPaid_WhenPending_ShouldTransitionCorrectly()
    {
        var order = Order.Create(Guid.NewGuid(), "key-pay");
        order.AddItem(Guid.NewGuid(), Guid.NewGuid(), 100m);

        order.MarkPaid();

        order.Status.Should().Be(OrderStatus.Paid);
    }

    [Fact]
    public void MarkPaid_WhenAlreadyPaid_ShouldThrow()
    {
        var order = Order.Create(Guid.NewGuid(), "key-dup");
        order.AddItem(Guid.NewGuid(), Guid.NewGuid(), 100m);
        order.MarkPaid();

        var act = () => order.MarkPaid();

        act.Should().Throw<OrderNotModifiableException>();
    }

    [Fact]
    public void Expire_WhenPending_ShouldMarkExpired_AndBeIdempotent()
    {
        var order = Order.Create(Guid.NewGuid(), "key-exp");

        order.Expire();
        order.Expire(); // second call is no-op

        order.Status.Should().Be(OrderStatus.Expired);
        // Only one OrderExpiredDomainEvent
        order.DomainEvents.Count(e =>
            e.GetType().Name == "OrderExpiredDomainEvent").Should().Be(1);
    }
}
