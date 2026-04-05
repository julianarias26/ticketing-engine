using System.Diagnostics;
using System.Text.Json;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using TicketingEngine.Application.Interfaces;
using TicketingEngine.Domain.Entities;
using TicketingEngine.Domain.Events;
using TicketingEngine.Domain.Exceptions;
using TicketingEngine.Domain.ValueObjects;

namespace TicketingEngine.Application.Commands.ReserveSeat;

public sealed record ReserveSeatCommand(
    Guid SeatId, Guid EventId, Guid UserId, string IdempotencyKey)
    : IRequest<ReserveSeatResult>;

public sealed record ReserveSeatResult(
    Guid OrderId, Guid ReservationId, DateTime ExpiresAt, decimal TotalAmount);

public sealed class ReserveSeatCommandValidator : AbstractValidator<ReserveSeatCommand>
{
    public ReserveSeatCommandValidator()
    {
        RuleFor(x => x.SeatId).NotEmpty();
        RuleFor(x => x.EventId).NotEmpty();
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.IdempotencyKey).NotEmpty().MaximumLength(128);
    }
}

public sealed class ReserveSeatCommandHandler
    : IRequestHandler<ReserveSeatCommand, ReserveSeatResult>
{
    private static readonly ActivitySource Activity = new("TicketingEngine");

    private readonly ISeatRepository        _seats;
    private readonly IOrderRepository       _orders;
    private readonly IReservationRepository _reservations;
    private readonly IOutboxRepository      _outbox;
    private readonly IAppDbContext          _db;
    private readonly IDistributedCache      _cache;
    private readonly ILogger<ReserveSeatCommandHandler> _logger;

    public ReserveSeatCommandHandler(
        ISeatRepository seats, IOrderRepository orders,
        IReservationRepository reservations, IOutboxRepository outbox,
        IAppDbContext db, IDistributedCache cache,
        ILogger<ReserveSeatCommandHandler> logger)
    {
        _seats = seats; _orders = orders; _reservations = reservations;
        _outbox = outbox; _db = db; _cache = cache; _logger = logger;
    }

    public async Task<ReserveSeatResult> Handle(
        ReserveSeatCommand cmd, CancellationToken ct)
    {
        using var activity = Activity.StartActivity("ReserveSeat");
        activity?.SetTag("seat.id",  cmd.SeatId.ToString());
        activity?.SetTag("event.id", cmd.EventId.ToString());

        // 1 — Idempotency check (Redis)
        var cacheKey = $"idempotency:{cmd.IdempotencyKey}";
        var cached   = await _cache.GetStringAsync(cacheKey, ct);
        if (cached is not null)
        {
            activity?.SetTag("cache.hit", true);
            return JsonSerializer.Deserialize<ReserveSeatResult>(cached)!;
        }

        // 2 — Pessimistic row-level lock (FOR UPDATE SKIP LOCKED)
        var seat = await _seats.GetByIdWithLockAsync(cmd.SeatId, ct)
            ?? throw new SeatNotFoundException(cmd.SeatId);

        if (seat.Status != SeatStatus.Available)
            throw new SeatAlreadyReservedException(cmd.SeatId);

        var price = seat.Section?.BasePrice
            ?? throw new InvalidOperationException("Section not loaded.");

        // 3 — Build aggregates
        var order = Order.Create(cmd.UserId, cmd.IdempotencyKey);
        order.AddItem(seat.Id, cmd.EventId, price);

        var reservation = Reservation.Create(
            seat.Id, cmd.UserId, order.Id, cmd.EventId,
            expiresAt: DateTime.UtcNow.AddMinutes(15));

        seat.Reserve();

        // 4 — Outbox message (same transaction)
        _outbox.Add(new OutboxMessage
        {
            AggregateType = nameof(Seat),
            EventType     = nameof(SeatReservedEvent),
            Payload       = JsonSerializer.Serialize(new SeatReservedEvent(
                seat.Id, cmd.EventId, cmd.UserId, order.Id, reservation.ExpiresAt))
        });

        _orders.Add(order);
        _reservations.Add(reservation);
        await _db.SaveChangesAsync(ct); // atomic commit

        var result = new ReserveSeatResult(
            order.Id, reservation.Id, reservation.ExpiresAt, order.TotalAmount);

        // 5 — Cache result for 24 h
        await _cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(result),
            new DistributedCacheEntryOptions
                { AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(24) }, ct);

        activity?.SetTag("order.id", order.Id.ToString());
        activity?.SetStatus(ActivityStatusCode.Ok);

        _logger.LogInformation(
            "Seat {SeatId} reserved. Order {OrderId}", seat.Id, order.Id);

        return result;
    }
}
