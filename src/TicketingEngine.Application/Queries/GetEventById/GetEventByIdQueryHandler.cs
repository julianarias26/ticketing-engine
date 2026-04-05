using MediatR;
using TicketingEngine.Application.Interfaces;
using TicketingEngine.Domain.Exceptions;

namespace TicketingEngine.Application.Queries.GetEventById;

public sealed record GetEventByIdQuery(Guid EventId) : IRequest<EventDetailDto>;

public sealed record EventDetailDto(Guid Id, string Name, string Description,
    DateTime EventDate, string Status, string VenueName, string VenueCity);

public sealed class GetEventByIdQueryHandler
    : IRequestHandler<GetEventByIdQuery, EventDetailDto>
{
    private readonly IEventRepository _events;
    private readonly ICacheService    _cache;

    public GetEventByIdQueryHandler(IEventRepository events, ICacheService cache)
    { _events = events; _cache = cache; }

    public async Task<EventDetailDto> Handle(
        GetEventByIdQuery query, CancellationToken ct)
    {
        var cacheKey = $"event:{query.EventId}";
        var cached   = await _cache.GetAsync<EventDetailDto>(cacheKey, ct);
        if (cached is not null) return cached;

        var ev = await _events.GetByIdAsync(query.EventId, ct)
            ?? throw new EventNotFoundException(query.EventId);

        var dto = new EventDetailDto(ev.Id, ev.Name, ev.Description, ev.EventDate,
            ev.Status.ToString(), ev.Venue?.Name ?? string.Empty,
            ev.Venue?.City ?? string.Empty);

        await _cache.SetAsync(cacheKey, dto, expiry: TimeSpan.FromMinutes(5), ct);
        return dto;
    }
}
