using Kairudev.Application.Sprint.Commands.RecordSprint;
using Kairudev.Application.Sprint.Queries.GetTodaySprints;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Monbsoft.BrilliantMediator.Abstractions;

namespace Kairudev.Api.Sprint;

[ApiController]
[Route("api/sprints")]
[Authorize]
public sealed class SprintsController : ControllerBase
{
    private readonly IMediator _mediator;

    public SprintsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>Records a completed or interrupted sprint session.</summary>
    [HttpPost]
    public async Task<IActionResult> RecordSprint(
        [FromBody] RecordSprintBody body,
        CancellationToken ct)
    {
        var command = new RecordSprintCommand(
            body.Name,
            body.StartedAt,
            body.EndedAt,
            body.Outcome,
            body.LinkedTaskId,
            body.SprintNumber);

        var result = await _mediator.DispatchAsync<RecordSprintCommand, RecordSprintResult>(command, ct);

        return result.IsSuccess
            ? Created($"api/sprints", result.Session)
            : BadRequest(new { error = result.Error });
    }

    /// <summary>Returns all sprint sessions recorded today for the current user.</summary>
    [HttpGet("today")]
    public async Task<IActionResult> GetTodaySprints(CancellationToken ct)
    {
        var result = await _mediator.SendAsync<GetTodaySprintsQuery, GetTodaySprintsResult>(new GetTodaySprintsQuery(), ct);
        return Ok(result.Sessions);
    }
}

public sealed record RecordSprintBody(
    string? Name,
    DateTimeOffset StartedAt,
    DateTimeOffset EndedAt,
    string Outcome,
    Guid? LinkedTaskId,
    int SprintNumber);
