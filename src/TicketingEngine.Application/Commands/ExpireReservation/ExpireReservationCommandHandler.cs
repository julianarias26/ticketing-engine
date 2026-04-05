using System.Text.Json;
using MediatR;
using Microsoft.Extensions.Logging;
using TicketingEngine.Application.Interfaces;
using TicketingEngine.Domain.Entities;
using TicketingEngine.Domain.Events;

namespace TicketingEngine.Application.Commands.ExpireReservation;

public sealed record ExpireReservationCommand(Guid SeatId, Guid OrderId) : IRequest;

public sealed class ExpireReservationCommandHandler
    : IRequestHandler<ExpireReservationCommand>
{
    private readonly IOrderRepository       _orders;
    private readonly IReservationRepository _reservations;
    private readonly ISeatRepository        _seats;
    private readonly IOutboxRepository      _outbox;
    private readonly IAppDbContext          _db;
    private readonly ILogger<ExpireReservationCommandHandler> _logger;

    public ExpireReservationCommandHandler(
        IOrderRepository orders, IReservationRepository reservations,
        ISeatRepository seats, IOutboxRepository outbox,
        IAppDbContext db, ILogger<ExpireReservationCommandHandler> logger)
    {
        _orders = orders; _reservations = reservations;
        _seats = seats; _outbox = outbox; _db = db; _logger = logger;
    }

    public async Task Handle(ExpireReservationCommand cmd, CancellationToken ct)
    {
        var reservation = await _reservations
            .GetBySeatAndOrderAsync(cmd.SeatId, cmd.OrderId, ct);
        if (reservation is null) return; // already handled

        var order = await _orders.GetByIdWithItemsAsync(cmd.OrderId, ct);
        if (order is null || !order.IsExpired()) return; // paid in time

        var seat = await _seats.GetByIdAsync(cmd.SeatId, ct);
        reservation.Expire();
        order.Expire();
        seat?.Release();

        var seatIds = order.Items.Select(i => i.SeatId).ToList();
        _outbox.Add(new OutboxMessage
        {
            AggregateType = nameof(Order),
            EventType     = nameof(OrderExpiredEvent),
            Payload       = JsonSerializer.Serialize(
                new OrderExpiredEvent(order.Id, order.UserId, seatIds))
        });

        await _db.SaveChangesAsync(ct);
        _logger.LogInformation("Order {OrderId} expired, seat {SeatId} released",
            cmd.OrderId, cmd.SeatId);
    }
}
