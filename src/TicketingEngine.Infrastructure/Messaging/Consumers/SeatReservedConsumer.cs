using MassTransit;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using TicketingEngine.Application.Commands.ExpireReservation;
using TicketingEngine.Domain.Events;
using MediatR;

namespace TicketingEngine.Infrastructure.Messaging.Consumers;

public sealed class SeatReservedConsumer : IConsumer<SeatReservedEvent>
{
    private readonly IHubContext<WaitingRoomHub> _hub;
    private readonly IMediator _mediator;
    private readonly ILogger<SeatReservedConsumer> _logger;

    public SeatReservedConsumer(
        IHubContext<WaitingRoomHub> hub, IMediator mediator,
        ILogger<SeatReservedConsumer> logger)
    { _hub = hub; _mediator = mediator; _logger = logger; }

    public async Task Consume(ConsumeContext<SeatReservedEvent> context)
    {
        var evt = context.Message;

        // Notify all clients watching this event
        await _hub.Clients.Group($"event:{evt.EventId}")
            .SendAsync("SeatStatusChanged", new
            {
                SeatId    = evt.SeatId,
                Status    = "Reserved",
                ExpiresAt = evt.ExpiresAt
            });

        _logger.LogInformation(
            "Seat {SeatId} reserved, SignalR notified for event {EventId}",
            evt.SeatId, evt.EventId);
    }
}

public sealed class OrderExpiredConsumer : IConsumer<OrderExpiredEvent>
{
    private readonly IHubContext<WaitingRoomHub> _hub;
    private readonly ILogger<OrderExpiredConsumer> _logger;

    public OrderExpiredConsumer(
        IHubContext<WaitingRoomHub> hub,
        ILogger<OrderExpiredConsumer> logger)
    { _hub = hub; _logger = logger; }

    public async Task Consume(ConsumeContext<OrderExpiredEvent> context)
    {
        var evt = context.Message;
        foreach (var seatId in evt.SeatIds)
        {
            await _hub.Clients.All.SendAsync("SeatStatusChanged", new
            {
                SeatId = seatId,
                Status = "Available"
            });
        }
        _logger.LogInformation("Order {OrderId} expired, {Count} seats released",
            evt.OrderId, evt.SeatIds.Count);
    }
}

// Placeholder hub reference — real hub lives in API layer
public class WaitingRoomHub : Hub { }
