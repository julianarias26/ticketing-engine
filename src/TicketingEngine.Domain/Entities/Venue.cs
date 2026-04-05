namespace TicketingEngine.Domain.Entities;

public sealed class Venue : BaseEntity
{
    public string Name { get; private set; } = default!;
    public string Address { get; private set; } = default!;
    public string City { get; private set; } = default!;
    public int TotalCapacity { get; private set; }
    public IReadOnlyCollection<Section> Sections => _sections.AsReadOnly();
    private readonly List<Section> _sections = [];

    private Venue() { }

    public static Venue Create(string name, string address, string city, int capacity) => new()
        { Name = name, Address = address, City = city, TotalCapacity = capacity };
}
