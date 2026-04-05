using TicketingEngine.Domain.ValueObjects;

namespace TicketingEngine.Domain.Entities;

public sealed class Event : BaseEntity
{
    public Guid VenueId { get; private set; }
    public string Name { get; private set; } = default!;
    public string Description { get; private set; } = default!;
    public DateTime EventDate { get; private set; }
    public EventStatus Status { get; private set; }
    public Venue? Venue { get; private set; }

    private Event() { }

    public static Event Create(Guid venueId, string name, string description, DateTime eventDate) => new()
    {
        VenueId     = venueId,
        Name        = name,
        Description = description,
        EventDate   = eventDate,
        Status      = EventStatus.Draft
    };

    public void Publish() => Status = EventStatus.OnSale;
    public void Cancel()  => Status = EventStatus.Cancelled;
}
