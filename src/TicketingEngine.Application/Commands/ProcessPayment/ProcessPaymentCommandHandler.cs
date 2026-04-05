using System.Text.Json;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using TicketingEngine.Application.Interfaces;
using TicketingEngine.Domain.Entities;
using TicketingEngine.Domain.Events;
using TicketingEngine.Domain.Exceptions;

namespace TicketingEngine.Application.Commands.ProcessPayment;

public sealed record ProcessPaymentCommand(
    Guid OrderId, Guid UserId, string Provider, string ProviderTransactionId)
    : IRequest<ProcessPaymentResult>;

public sealed record ProcessPaymentResult(Guid PaymentId, string Status, DateTime ProcessedAt);

public sealed class ProcessPaymentCommandValidator : AbstractValidator<ProcessPaymentCommand>
{
    public ProcessPaymentCommandValidator()
    {
        RuleFor(x => x.OrderId).NotEmpty();
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.Provider).NotEmpty().MaximumLength(50);
        RuleFor(x => x.ProviderTransactionId).NotEmpty().MaximumLength(256);
    }
}

public sealed class ProcessPaymentCommandHandler
    : IRequestHandler<ProcessPaymentCommand, ProcessPaymentResult>
{
    private readonly IOrderRepository       _orders;
    private readonly IReservationRepository _reservations;
    private readonly ISeatRepository        _seats;
    private readonly IOutboxRepository      _outbox;
    private readonly IAppDbContext          _db;
    private readonly ILogger<ProcessPaymentCommandHandler> _logger;

    public ProcessPaymentCommandHandler(
        IOrderRepository orders, IReservationRepository reservations,
        ISeatRepository seats, IOutboxRepository outbox,
        IAppDbContext db, ILogger<ProcessPaymentCommandHandler> logger)
    {
        _orders = orders; _reservations = reservations;
        _seats = seats; _outbox = outbox; _db = db; _logger = logger;
    }

    public async Task<ProcessPaymentResult> Handle(
        ProcessPaymentCommand cmd, CancellationToken ct)
    {
        var order = await _orders.GetByIdWithItemsAsync(cmd.OrderId, ct)
            ?? throw new OrderNotFoundException(cmd.OrderId);

        if (order.UserId != cmd.UserId)
            throw new UnauthorizedAccessException(
                $"User {cmd.UserId} does not own order {cmd.OrderId}.");

        if (order.IsExpired())
        {
            order.Expire();
            await _db.SaveChangesAsync(ct);
            throw new InvalidOperationException($"Order {cmd.OrderId} has expired.");
        }

        var payment = Payment.Create(cmd.OrderId, cmd.Provider,
            cmd.ProviderTransactionId, order.TotalAmount);
        payment.MarkSucceeded();
        order.MarkPaid();

        foreach (var item in order.Items)
        {
            var reservation = await _reservations
                .GetBySeatAndOrderAsync(item.SeatId, order.Id, ct);
            reservation?.Confirm();

            var seat = await _seats.GetByIdAsync(item.SeatId, ct);
            seat?.MarkSold();
        }

        _outbox.Add(new OutboxMessage
        {
            AggregateType = nameof(Order),
            EventType     = nameof(PaymentProcessedEvent),
            Payload       = JsonSerializer.Serialize(new PaymentProcessedEvent(
                order.Id, order.UserId, order.TotalAmount, cmd.Provider))
        });

        await _db.SaveChangesAsync(ct);
        _logger.LogInformation("Payment {PaymentId} OK for order {OrderId}",
            payment.Id, cmd.OrderId);

        return new ProcessPaymentResult(payment.Id, "Succeeded", payment.ProcessedAt!.Value);
    }
}
