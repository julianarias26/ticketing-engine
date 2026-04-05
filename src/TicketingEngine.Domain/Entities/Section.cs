namespace TicketingEngine.Domain.Entities;

public sealed class Section : BaseEntity
{
    public Guid VenueId { get; private set; }
    public string Name { get; private set; } = default!;
    public int Capacity { get; private set; }
    public decimal BasePrice { get; private set; }
    public Venue? Venue { get; private set; }
    public IReadOnlyCollection<Seat> Seats => _seats.AsReadOnly();
    private readonly List<Seat> _seats = [];

    private Section() { }

    public static Section Create(Guid venueId, string name, int capacity, decimal basePrice) => new()
        { VenueId = venueId, Name = name, Capacity = capacity, BasePrice = basePrice };
}
