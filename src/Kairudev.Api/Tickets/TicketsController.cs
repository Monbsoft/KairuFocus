using Kairudev.Application.Tickets.Queries.GetAssignedJiraTickets;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Monbsoft.BrilliantMediator.Abstractions;

namespace Kairudev.Api.Tickets;

[ApiController]
[Route("api/tickets")]
[Authorize]
public sealed class TicketsController : ControllerBase
{
    private readonly IMediator _mediator;

    public TicketsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("assigned")]
    public async Task<IActionResult> GetAssigned(CancellationToken ct)
    {
        var result = await _mediator.SendAsync<GetAssignedJiraTicketsQuery, GetAssignedJiraTicketsResult>(new GetAssignedJiraTicketsQuery(), ct);

        return result switch
        {
            { IsSuccess: true } => Ok(result.Tickets),
            { IsNotConfigured: true } => Ok(new { notConfigured = true, tickets = Array.Empty<object>() }),
            _ => StatusCode(502, new { error = result.Error })
        };
    }
}
