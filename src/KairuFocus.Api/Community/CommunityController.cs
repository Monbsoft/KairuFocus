using System.ComponentModel.DataAnnotations;
using KairuFocus.Application.Community.Commands.ChangeProfileName;
using KairuFocus.Application.Community.Commands.JoinCommunity;
using KairuFocus.Application.Community.Commands.JoinOpenSprint;
using KairuFocus.Application.Community.Commands.LeaveCommunity;
using KairuFocus.Application.Community.Commands.SetSprintClosed;
using KairuFocus.Application.Community.Common;
using KairuFocus.Application.Community.Queries.GetCommunityMembership;
using KairuFocus.Application.Community.Queries.GetMemberProfile;
using KairuFocus.Application.Community.Queries.GetOpenSprints;
using KairuFocus.Application.Community.Queries.GetSprintParticipants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Monbsoft.BrilliantMediator.Abstractions;
using CommunityErrors = KairuFocus.Domain.Community.DomainErrors;
using PomodoroErrors = KairuFocus.Domain.Pomodoro.DomainErrors;

namespace KairuFocus.Api.Community;

/// <summary>
/// Profile, membership and open sprints of the community (spec §6.5). Error bodies are <c>{ error, code }</c>;
/// no response carries a user or session identifier.
/// </summary>
[ApiController]
[Route("api/community")]
[Authorize]
public sealed class CommunityController : ControllerBase
{
    // One text for every refusal of a name: it never tells why (reserved, another user's login, taken).
    private const string NameUnavailableText = "This name is not available.";
    private const string UnexpectedErrorText = "An unexpected error occurred.";

    private readonly IMediator _mediator;

    public CommunityController(IMediator mediator)
    {
        _mediator = mediator;
    }

    // ── Membership (UC-CO-01 to UC-CO-03) ──────────────────────────────────

    [HttpGet("membership")]
    public async Task<IActionResult> GetMembership(CancellationToken ct)
    {
        var result = await _mediator.SendAsync<GetCommunityMembershipQuery, MembershipViewModel>(
            new GetCommunityMembershipQuery(), ct);
        return Ok(result);
    }

    [HttpPost("membership")]
    public async Task<IActionResult> Join([FromBody] ProfileNameBody body, CancellationToken ct)
    {
        if (!ProfileNameSources.TryParse(body.Source, out var source))
            return InvalidName();

        var result = await _mediator.DispatchAsync<JoinCommunityCommand, JoinCommunityResult>(
            new JoinCommunityCommand(source, body.Name), ct);

        return result.IsSuccess
            ? CreatedAtAction(nameof(GetMembership), result.Membership)
            : ErrorResponse(result.Error!);
    }

    [HttpPut("membership/name")]
    public async Task<IActionResult> ChangeName([FromBody] ProfileNameBody body, CancellationToken ct)
    {
        if (!ProfileNameSources.TryParse(body.Source, out var source))
            return InvalidName();

        var result = await _mediator.DispatchAsync<ChangeProfileNameCommand, ChangeProfileNameResult>(
            new ChangeProfileNameCommand(source, body.Name), ct);

        return result.IsSuccess
            ? Ok(result.Membership)
            : ErrorResponse(result.Error!, notMemberStatus: StatusCodes.Status404NotFound);
    }

    [HttpDelete("membership")]
    public async Task<IActionResult> Leave(CancellationToken ct)
    {
        var result = await _mediator.DispatchAsync<LeaveCommunityCommand, LeaveCommunityResult>(
            new LeaveCommunityCommand(), ct);

        return result.IsSuccess
            ? NoContent()
            : ErrorResponse(result.Error!, notMemberStatus: StatusCodes.Status404NotFound);
    }

    // ── Profiles and open sprints (UC-CO-04 to UC-CO-08) ───────────────────

    [HttpGet("members/{name}")]
    public async Task<IActionResult> GetMember(string name, CancellationToken ct)
    {
        var result = await _mediator.SendAsync<GetMemberProfileQuery, GetMemberProfileResult>(
            new GetMemberProfileQuery(name), ct);

        // Same empty 404 for a malformed, unknown or non-activated name.
        return result is { Found: true, Profile: not null }
            ? Ok(result.Profile)
            : NotFound();
    }

    [HttpGet("open-sprints")]
    public async Task<IActionResult> GetOpenSprints(CancellationToken ct)
    {
        var result = await _mediator.SendAsync<GetOpenSprintsQuery, GetOpenSprintsResult>(
            new GetOpenSprintsQuery(), ct);
        return Ok(result);
    }

    [HttpPost("open-sprints/{name}/join")]
    public async Task<IActionResult> JoinOpenSprint(string name, CancellationToken ct)
    {
        var result = await _mediator.DispatchAsync<JoinOpenSprintCommand, JoinOpenSprintResult>(
            new JoinOpenSprintCommand(name), ct);

        return result.IsSuccess
            ? StatusCode(StatusCodes.Status201Created, new { sharedEndAt = result.SharedEndAt })
            : ErrorResponse(result.Error!, notMemberStatus: StatusCodes.Status403Forbidden);
    }

    [HttpGet("current-sprint/participants")]
    public async Task<IActionResult> GetParticipants(CancellationToken ct)
    {
        var result = await _mediator.SendAsync<GetSprintParticipantsQuery, GetSprintParticipantsResult>(
            new GetSprintParticipantsQuery(), ct);
        return Ok(result);
    }

    [HttpPut("current-sprint/closed")]
    public async Task<IActionResult> SetSprintClosed([FromBody] SprintClosedBody body, CancellationToken ct)
    {
        var result = await _mediator.DispatchAsync<SetSprintClosedCommand, SetSprintClosedResult>(
            new SetSprintClosedCommand(body.Closed!.Value), ct);

        return result.IsSuccess
            ? NoContent()
            : ErrorResponse(result.Error!);
    }

    // ── Error mapping (spec §8) ────────────────────────────────────────────

    private IActionResult InvalidName() =>
        BadRequest(new { error = CommunityErrors.Community.InvalidName, code = "invalid-name" });

    /// <param name="notMemberStatus">
    /// Status of <c>NotMember</c>, which depends on the operation: 404 for the membership itself,
    /// 403 for joining a sprint.
    /// </param>
    private ObjectResult ErrorResponse(string error, int notMemberStatus = StatusCodes.Status404NotFound) =>
        error switch
        {
            CommunityErrors.Community.InvalidName =>
                Body(StatusCodes.Status400BadRequest, error, "invalid-name"),
            CommunityErrors.Community.ReservedName or CommunityErrors.Community.LoginOfAnotherUser =>
                Body(StatusCodes.Status400BadRequest, NameUnavailableText, "name-unavailable"),
            CommunityErrors.Community.NameTaken =>
                Body(StatusCodes.Status409Conflict, NameUnavailableText, "name-unavailable"),
            CommunityErrors.Community.AlreadyMember =>
                Body(StatusCodes.Status409Conflict, error, "already-member"),
            CommunityErrors.Community.NotMember =>
                Body(notMemberStatus, error, "not-member"),
            PomodoroErrors.Pomodoro.SessionAlreadyActive or PomodoroErrors.Pomodoro.ConcurrentSessionStart =>
                Body(StatusCodes.Status409Conflict, error, "sprint-active"),
            CommunityErrors.Community.CannotJoinOwnSprint =>
                Body(StatusCodes.Status409Conflict, error, "own-sprint"),
            CommunityErrors.Community.SprintNotJoinable =>
                Body(StatusCodes.Status410Gone, error, "sprint-closed"),
            CommunityErrors.Community.NotSprintHost =>
                Body(StatusCodes.Status403Forbidden, error, "not-host"),
            CommunityErrors.Community.NoActiveSprint =>
                Body(StatusCodes.Status404NotFound, error, "no-active-sprint"),
            // Not in the contract (UserNotFound, for example): nothing to tell the client.
            _ => StatusCode(StatusCodes.Status500InternalServerError, new { error = UnexpectedErrorText })
        };

    private ObjectResult Body(int status, string error, string code) =>
        StatusCode(status, new { error, code });
}

/// <summary>Both fields are nullable: a missing or unknown source is answered by <c>invalid-name</c>, not by a generic 400.</summary>
public sealed record ProfileNameBody(string? Source, string? Name);

public sealed record SprintClosedBody([Required] bool? Closed);
