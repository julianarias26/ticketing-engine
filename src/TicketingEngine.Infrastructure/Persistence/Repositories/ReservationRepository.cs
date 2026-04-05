using Microsoft.EntityFrameworkCore;
using TicketingEngine.Application.Interfaces;
using TicketingEngine.Domain.Entities;
using TicketingEngine.Domain.ValueObjects;

namespace TicketingEngine.Infrastructure.Persistence.Repositories;

public sealed class ReservationRepository : IReservationRepository
{
    private readonly AppDbContext _db;
    public ReservationRepository(AppDbContext db) => _db = db;

    public Task<Reservation?> GetByIdAsync(Guid id, CancellationToken ct) =>
        _db.Reservations.FirstOrDefaultAsync(r => r.Id == id, ct);

    public Task<Reservation?> GetBySeatAndOrderAsync(
        Guid seatId, Guid orderId, CancellationToken ct) =>
        _db.Reservations.FirstOrDefaultAsync(
            r => r.SeatId == seatId && r.OrderId == orderId
              && r.Status == ReservationStatus.Pending, ct);

    public void Add(Reservation reservation) => _db.Reservations.Add(reservation);
}
