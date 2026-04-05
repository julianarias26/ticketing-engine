using Hangfire;
using MediatR;
using Microsoft.Extensions.Logging;
using TicketingEngine.Application.Commands.ExpireReservation;

namespace TicketingEngine.Infrastructure.Jobs;

public sealed class ReservationExpiryJob
{
    private readonly IMediator _mediator;
    private readonly ILogger<ReservationExpiryJob> _logger;

    public ReservationExpiryJob(IMediator mediator,
        ILogger<ReservationExpiryJob> logger)
    { _mediator = mediator; _logger = logger; }

    [AutomaticRetry(Attempts = 3)]
    public async Task ExpireAsync(Guid seatId, Guid orderId)
    {
        _logger.LogInformation(
            "Running expiry job for seat {SeatId} / order {OrderId}",
            seatId, orderId);
        await _mediator.Send(new ExpireReservationCommand(seatId, orderId));
    }
}
