using KairuFocus.Application.Tasks.Commands.AddTask;
using KairuFocus.Application.Tasks.Commands.ChangeTaskStatus;
using KairuFocus.Application.Tasks.Commands.CompleteTask;
using KairuFocus.Application.Tasks.Commands.DeleteTask;
using KairuFocus.Application.Tasks.Commands.LinkJiraTicket;
using KairuFocus.Application.Tasks.Commands.UnlinkJiraTicket;
using KairuFocus.Application.Tasks.Commands.UpdateTask;
using KairuFocus.Application.Tasks.Queries.GetTaskById;
using KairuFocus.Application.Tasks.Queries.ListTasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Monbsoft.BrilliantMediator.Abstractions;

namespace KairuFocus.Api.Tasks;

[ApiController]
[Route("api/tasks")]
[Authorize]
public sealed class TasksController : ControllerBase
{
    private readonly IMediator _mediator;

    public TasksController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? search = null,
        [FromQuery] TaskStatusFilter status = TaskStatusFilter.OpenOnly,
        CancellationToken ct = default)
    {
        var result = await _mediator.SendAsync<ListTasksQuery, ListTasksResult>(
            new ListTasksQuery(search, status), ct);
        return Ok(result.Tasks);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var result = await _mediator.SendAsync<GetTaskByIdQuery, GetTaskByIdResult>(new GetTaskByIdQuery(id), ct);
        return result.Task is null ? NotFound() : Ok(result.Task);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] AddTaskCommand command, CancellationToken ct)
    {
        var result = await _mediator.DispatchAsync<AddTaskCommand, AddTaskResult>(command, ct);

        return result.IsSuccess
            ? Created($"api/tasks/{result.Task!.Id}", result.Task)
            : BadRequest(new { error = result.Error });
    }

    [HttpPut("{id:guid}/complete")]
    public async Task<IActionResult> Complete(Guid id, CancellationToken ct)
    {
        var result = await _mediator.DispatchAsync<CompleteTaskCommand, CompleteTaskResult>(new CompleteTaskCommand(id), ct);

        return result switch
        {
            { IsSuccess: true } => NoContent(),
            { IsNotFound: true } => NotFound(),
            _ => BadRequest(new { error = result.Error })
        };
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var result = await _mediator.DispatchAsync<DeleteTaskCommand, DeleteTaskResult>(new DeleteTaskCommand(id), ct);

        return result switch
        {
            { IsSuccess: true } => NoContent(),
            { IsNotFound: true } => NotFound(),
            _ => StatusCode(500, new { error = result.Error })
        };
    }

    [HttpPatch("{id:guid}/status")]
    public async Task<IActionResult> ChangeStatus(
        Guid id,
        [FromBody] ChangeTaskStatusBody body,
        CancellationToken ct)
    {
        var result = await _mediator.DispatchAsync<ChangeTaskStatusCommand, ChangeTaskStatusResult>(
            new ChangeTaskStatusCommand(id, body.NewStatus), ct);

        return result switch
        {
            { IsSuccess: true } => Ok(result.Task),
            { IsNotFound: true } => NotFound(),
            { ValidationError: not null } => BadRequest(new { error = result.ValidationError }),
            { ConflictError: not null } => Conflict(new { error = result.ConflictError }),
            _ => StatusCode(500)
        };
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] UpdateTaskBody body,
        CancellationToken ct)
    {
        var result = await _mediator.DispatchAsync<UpdateTaskCommand, UpdateTaskResult>(
            new UpdateTaskCommand(id, body.Title, body.Description, body.Tags), ct);

        return result switch
        {
            { IsSuccess: true } => Ok(result.Task),
            { IsNotFound: true } => NotFound(),
            { Error: not null } => BadRequest(new { error = result.Error }),
            _ => StatusCode(500)
        };
    }

    [HttpPut("{id:guid}/jira-ticket")]
    public async Task<IActionResult> LinkJiraTicket(
        Guid id,
        [FromBody] LinkJiraTicketBody body,
        CancellationToken ct)
    {
        var result = await _mediator.DispatchAsync<LinkJiraTicketCommand, LinkJiraTicketResult>(
            new LinkJiraTicketCommand(id, body.JiraTicketKey), ct);

        return result switch
        {
            { IsSuccess: true } => NoContent(),
            { IsNotFound: true } => NotFound(),
            _ => BadRequest(new { error = result.Error })
        };
    }

    [HttpDelete("{id:guid}/jira-ticket")]
    public async Task<IActionResult> UnlinkJiraTicket(Guid id, CancellationToken ct)
    {
        var result = await _mediator.DispatchAsync<UnlinkJiraTicketCommand, UnlinkJiraTicketResult>(
            new UnlinkJiraTicketCommand(id), ct);

        return result switch
        {
            { IsSuccess: true } => NoContent(),
            { IsNotFound: true } => NotFound(),
            _ => StatusCode(500)
        };
    }
}

public sealed record ChangeTaskStatusBody(string NewStatus);
public sealed record UpdateTaskBody(string Title, string? Description, List<string>? Tags = null);
public sealed record LinkJiraTicketBody(string JiraTicketKey);
