using System.Globalization;
using Kairudev.Application.Journal.Commands.AddComment;
using Kairudev.Application.Journal.Commands.RemoveComment;
using Kairudev.Application.Journal.Commands.UpdateComment;
using Kairudev.Application.Journal.Queries.GetJournalByDate;
using Kairudev.Application.Journal.Queries.GetTodayJournal;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Monbsoft.BrilliantMediator.Abstractions;

namespace Kairudev.Api.Journal;

[ApiController]
[Route("api/journal")]
[Authorize]
public sealed class JournalController : ControllerBase
{
    private readonly IMediator _mediator;

    public JournalController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("today")]
    public async Task<IActionResult> GetToday(CancellationToken ct)
    {
        var result = await _mediator.SendAsync<GetTodayJournalQuery, GetTodayJournalResult>(new GetTodayJournalQuery(), ct);
        return Ok(result.Entries);
    }

    [HttpGet("date/{date}")]
    public async Task<IActionResult> GetByDate(string date, CancellationToken ct)
    {
        if (!DateOnly.TryParseExact(date, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedDate))
            return BadRequest("Format de date invalide. Utiliser yyyy-MM-dd.");

        var result = await _mediator.SendAsync<GetJournalByDateQuery, GetJournalByDateResult>(new GetJournalByDateQuery(parsedDate), ct);
        return Ok(result.Entries);
    }

    [HttpPost("{entryId:guid}/comments")]
    public async Task<IActionResult> AddComment(
        Guid entryId,
        [FromBody] AddCommentBody body,
        CancellationToken ct)
    {
        var result = await _mediator.DispatchAsync<AddCommentCommand, AddCommentResult>(new AddCommentCommand(entryId, body.Text), ct);

        return result switch
        {
            { IsSuccess: true } => Ok(result.Entry),
            { IsNotFound: true } => NotFound(),
            { ValidationError: not null } => BadRequest(new { error = result.ValidationError }),
            _ => StatusCode(500)
        };
    }

    [HttpPut("{entryId:guid}/comments/{commentId:guid}")]
    public async Task<IActionResult> UpdateComment(
        Guid entryId,
        Guid commentId,
        [FromBody] UpdateCommentBody body,
        CancellationToken ct)
    {
        var result = await _mediator.DispatchAsync<UpdateCommentCommand, UpdateCommentResult>(
            new UpdateCommentCommand(entryId, commentId, body.Text), ct);

        return result switch
        {
            { IsSuccess: true } => Ok(result.Entry),
            { IsNotFound: true } => NotFound(),
            { ValidationError: not null } => BadRequest(new { error = result.ValidationError }),
            _ => StatusCode(500)
        };
    }

    [HttpDelete("{entryId:guid}/comments/{commentId:guid}")]
    public async Task<IActionResult> RemoveComment(
        Guid entryId,
        Guid commentId,
        CancellationToken ct)
    {
        var result = await _mediator.DispatchAsync<RemoveCommentCommand, RemoveCommentResult>(
            new RemoveCommentCommand(entryId, commentId), ct);

        return result switch
        {
            { IsSuccess: true } => NoContent(),
            { IsNotFound: true } => NotFound(),
            _ => StatusCode(500)
        };
    }
}

public sealed record AddCommentBody(string Text);
public sealed record UpdateCommentBody(string Text);
