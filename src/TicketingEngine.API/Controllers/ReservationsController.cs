using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TicketingEngine.Application.Commands.ReserveSeat;
using TicketingEngine.Application.Queries.GetAvailableSeats;

namespace TicketingEngine.API.Controllers;

[ApiController]
[ApiVersion(1)]
[Route("api/v{version:apiVersion}/[controller]")]
[Authorize]
public sealed class ReservationsController : ControllerBase
{
    private readonly IMediator _mediator;

    public ReservationsController(IMediator mediator) => _mediator = mediator;

    /// <summary>Reserve a seat for an event.</summary>
    /// <remarks>
    /// Idempotent — repeat requests with the same IdempotencyKey return
    /// the cached response without creating a duplicate reservation.
    /// </remarks>
    [HttpPost]
    [ProducesResponseType(typeof(ReserveSeatResult), 200)]
    [ProducesResponseType(409)]
    [ProducesResponseType(422)]
    public async Task<IActionResult> Reserve(
        [FromBody] ReserveSeatRequest request,
        CancellationToken ct)
    {
        var userId = Guid.Parse(
            User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        var result = await _mediator.Send(new ReserveSeatCommand(
            request.SeatId,
            request.EventId,
            userId,
            request.IdempotencyKey), ct);

        return Ok(result);
    }

    /// <summary>Get available seats for an event.</summary>
    [HttpGet("events/{eventId:guid}/seats")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(GetAvailableSeatsResult), 200)]
    public async Task<IActionResult> GetAvailableSeats(
        Guid eventId, CancellationToken ct)
    {
        var result = await _mediator.Send(
            new GetAvailableSeatsQuery(eventId), ct);
        return Ok(result);
    }
}

public sealed record ReserveSeatRequest(
    Guid SeatId,
    Guid EventId,
    string IdempotencyKey);
