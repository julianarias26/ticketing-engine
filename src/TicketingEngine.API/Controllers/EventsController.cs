using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using TicketingEngine.Application.Queries.GetEventById;

namespace TicketingEngine.API.Controllers;

[ApiController]
[ApiVersion(1)]
[Route("api/v{version:apiVersion}/[controller]")]
public sealed class EventsController : ControllerBase
{
    private readonly IMediator _mediator;
    public EventsController(IMediator mediator) => _mediator = mediator;

    /// <summary>Get event details by ID.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(EventDetailDto), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetEventByIdQuery(id), ct);
        return Ok(result);
    }
}
