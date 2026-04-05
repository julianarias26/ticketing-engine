using MediatR;
using TicketingEngine.Application.Interfaces;
using TicketingEngine.Domain.Exceptions;

namespace TicketingEngine.Application.Queries.GetAvailableSeats;

public sealed record GetAvailableSeatsQuery(Guid EventId)
    : IRequest<GetAvailableSeatsResult>;

public sealed record SeatDto(Guid Id, string RowLabel, int SeatNumber,
    string Status, decimal Price, string SectionName);

public sealed record GetAvailableSeatsResult(
    Guid EventId, int TotalAvailable, IReadOnlyList<SeatDto> Seats);

public sealed class GetAvailableSeatsQueryHandler
    : IRequestHandler<GetAvailableSeatsQuery, GetAvailableSeatsResult>
{
    private readonly ISeatRepository  _seats;
    private readonly IEventRepository _events;
    private readonly ICacheService    _cache;

    public GetAvailableSeatsQueryHandler(
        ISeatRepository seats, IEventRepository events, ICacheService cache)
    { _seats = seats; _events = events; _cache = cache; }

    public async Task<GetAvailableSeatsResult> Handle(
        GetAvailableSeatsQuery query, CancellationToken ct)
    {
        var cacheKey = $"seats:available:{query.EventId}";
        var cached   = await _cache.GetAsync<GetAvailableSeatsResult>(cacheKey, ct);
        if (cached is not null) return cached;

        _ = await _events.GetByIdAsync(query.EventId, ct)
            ?? throw new EventNotFoundException(query.EventId);

        var seats = await _seats.GetAvailableByEventAsync(query.EventId, ct);
        var dtos  = seats.Select(s => new SeatDto(
            s.Id, s.RowLabel, s.SeatNumber, s.Status.ToString(),
            s.Section?.BasePrice ?? 0, s.Section?.Name ?? string.Empty)).ToList();

        var result = new GetAvailableSeatsResult(query.EventId, dtos.Count, dtos);

        // Short TTL — seats change rapidly during on-sale
        await _cache.SetAsync(cacheKey, result, expiry: TimeSpan.FromSeconds(5), ct);
        return result;
    }
}
