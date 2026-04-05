using TicketingEngine.Domain.Entities;
using TicketingEngine.Domain.ValueObjects;

namespace TicketingEngine.UnitTests.Builders;

/// <summary>Test Data Builder for Seat — makes tests readable.</summary>
public sealed class SeatBuilder
{
    private Guid       _sectionId  = Guid.NewGuid();
    private string     _rowLabel   = "A";
    private int        _seatNumber = 1;
    private SeatStatus _status     = SeatStatus.Available;

    public SeatBuilder WithSectionId(Guid id)          { _sectionId  = id;     return this; }
    public SeatBuilder WithRow(string row)              { _rowLabel   = row;    return this; }
    public SeatBuilder WithNumber(int n)                { _seatNumber = n;      return this; }
    public SeatBuilder WithStatus(SeatStatus s)         { _status     = s;      return this; }
    public SeatBuilder AsAvailable()                    => WithStatus(SeatStatus.Available);
    public SeatBuilder AsReserved()                     => WithStatus(SeatStatus.Reserved);

    public Seat Build() =>
        Seat.Create(_sectionId, _rowLabel, _seatNumber, _status);
}
