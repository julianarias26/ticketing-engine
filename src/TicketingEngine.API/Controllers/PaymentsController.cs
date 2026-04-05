using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TicketingEngine.Application.Commands.ProcessPayment;

namespace TicketingEngine.API.Controllers;

[ApiController]
[ApiVersion(1)]
[Route("api/v{version:apiVersion}/[controller]")]
[Authorize]
public sealed class PaymentsController : ControllerBase
{
    private readonly IMediator _mediator;
    public PaymentsController(IMediator mediator) => _mediator = mediator;

    /// <summary>Process payment for a pending order.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(ProcessPaymentResult), 200)]
    [ProducesResponseType(404)]
    [ProducesResponseType(422)]
    public async Task<IActionResult> ProcessPayment(
        [FromBody] ProcessPaymentRequest request,
        CancellationToken ct)
    {
        var userId = Guid.Parse(
            User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        var result = await _mediator.Send(new ProcessPaymentCommand(
            request.OrderId,
            userId,
            request.Provider,
            request.ProviderTransactionId), ct);

        return Ok(result);
    }
}

public sealed record ProcessPaymentRequest(
    Guid OrderId,
    string Provider,
    string ProviderTransactionId);
