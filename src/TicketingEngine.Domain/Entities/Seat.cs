using TicketingEngine.Domain.Events;
using TicketingEngine.Domain.Exceptions;
using TicketingEngine.Domain.ValueObjects;

namespace TicketingEngine.Domain.Entities;

public sealed class Seat : BaseEntity
{
    public Guid SectionId { get; private set; }
    public string RowLabel { get; private set; } = default!;
    public int SeatNumber { get; private set; }
    public SeatStatus Status { get; private set; }
    public int Version { get; private set; }
    public Section? Section { get; private set; }

    private Seat() { }

    public static Seat Create(Guid sectionId, string rowLabel, int seatNumber,
        SeatStatus status = SeatStatus.Available)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(rowLabel);
        if (seatNumber <= 0) throw new ArgumentOutOfRangeException(nameof(seatNumber));
        return new Seat
        {
            SectionId  = sectionId,
            RowLabel   = rowLabel.ToUpperInvariant(),
            SeatNumber = seatNumber,
            Status     = status,
            Version    = 0
        };
    }

    public void Reserve()
    {
        if (Status != SeatStatus.Available)
            throw new SeatAlreadyReservedException(Id);
        Status = SeatStatus.Reserved;
        Version++;
        AddDomainEvent(new SeatReservedDomainEvent(Id, SectionId));
    }

    public void Release()
    {
        if (Status == SeatStatus.Available) return;
        Status = SeatStatus.Available;
        Version++;
        AddDomainEvent(new SeatReleasedDomainEvent(Id, SectionId));
    }

    public void MarkSold()
    {
        if (Status != SeatStatus.Reserved)
            throw new InvalidSeatStatusTransitionException(Id, Status, SeatStatus.Sold);
        Status = SeatStatus.Sold;
        Version++;
    }
}
