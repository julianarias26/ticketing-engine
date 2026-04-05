using MediatR;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace TicketingEngine.Application.Commands.ReserveSeat
{
    // Application/Commands/ReserveSeat/ReserveSeatCommand.cs
    public record ReserveSeatCommand(
        Guid SeatId,
        Guid EventId,
        Guid UserId,
        string IdempotencyKey) : IRequest<ReserveSeatResult>;

    // Application/Commands/ReserveSeat/ReserveSeatCommandHandler.cs
    public sealed class ReserveSeatCommandHandler
        : IRequestHandler<ReserveSeatCommand, ReserveSeatResult>
    {
        private readonly AppDbContext _db;
        private readonly IDistributedCache _cache;
        private readonly ILogger<ReserveSeatCommandHandler> _logger;

        public ReserveSeatCommandHandler(
            AppDbContext db,
            IDistributedCache cache,
            ILogger<ReserveSeatCommandHandler> logger)
        {
            _db = db;
            _cache = cache;
            _logger = logger;
        }

        public async Task<ReserveSeatResult> Handle(
            ReserveSeatCommand cmd,
            CancellationToken ct)
        {
            // 1. Idempotency check en Redis (O(1), evita hit a la DB)
            var cached = await _cache.GetStringAsync(
                $"idempotency:{cmd.IdempotencyKey}", ct);
            if (cached is not null)
                return JsonSerializer.Deserialize<ReserveSeatResult>(cached)!;

            // 2. Pessimistic lock a nivel de fila con FOR UPDATE SKIP LOCKED
            //    → otros procesos saltan este asiento en vez de bloquearse
            var seat = await _db.Seats
                .FromSqlRaw("""
                SELECT * FROM seats
                WHERE id = {0}
                FOR UPDATE SKIP LOCKED
                """, cmd.SeatId)
                .FirstOrDefaultAsync(ct)
                ?? throw new SeatNotFoundException(cmd.SeatId);

            if (seat.Status != SeatStatus.Available)
                throw new SeatAlreadyReservedException(cmd.SeatId);

            // 3. Crear Order + Reservation en la misma transacción
            await using var tx = await _db.Database.BeginTransactionAsync(ct);
            try
            {
                var order = Order.Create(cmd.UserId, cmd.IdempotencyKey);
                var reservation = Reservation.Create(
                    seat.Id, cmd.UserId, order.Id,
                    expiresAt: DateTime.UtcNow.AddMinutes(15));

                seat.Reserve(); // cambia Status = Reserved, incrementa Version

                _db.Orders.Add(order);
                _db.Reservations.Add(reservation);

                // 4. Outbox Pattern: guardar evento en la misma transacción
                _db.OutboxMessages.Add(new OutboxMessage
                {
                    AggregateType = nameof(Seat),
                    EventType = nameof(SeatReservedEvent),
                    Payload = JsonSerializer.Serialize(new SeatReservedEvent(
                        seat.Id, cmd.EventId, cmd.UserId, reservation.ExpiresAt))
                });

                await _db.SaveChangesAsync(ct); // atomic commit
                await tx.CommitAsync(ct);

                var result = new ReserveSeatResult(order.Id, reservation.ExpiresAt);

                // 5. Cachear resultado para idempotency (TTL = 24h)
                await _cache.SetStringAsync(
                    $"idempotency:{cmd.IdempotencyKey}",
                    JsonSerializer.Serialize(result),
                    new DistributedCacheEntryOptions
                    {
                        AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(24)
                    }, ct);

                return result;
            }
            catch
            {
                await tx.RollbackAsync(ct);
                throw;
            }
        }
    }
}
