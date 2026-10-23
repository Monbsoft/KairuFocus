using KairuFocus.Application.Gamification.Commands.JoinLeaderboard;
using KairuFocus.Application.Gamification.Commands.LeaveLeaderboard;
using KairuFocus.Application.Gamification.Commands.RerollPseudonym;
using KairuFocus.Application.Gamification.Common;
using KairuFocus.Application.Gamification.Queries.GetCommunityLeaderboard;
using KairuFocus.Application.Gamification.Queries.GetCommunityPresence;
using KairuFocus.Application.Gamification.Queries.GetCommunityProgress;
using KairuFocus.Application.Gamification.Queries.GetCommunityRecap;
using KairuFocus.Application.Gamification.Queries.GetLeaderboardMembership;
using KairuFocus.Application.Gamification.Queries.GetXpSummary;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Monbsoft.BrilliantMediator.Abstractions;
using GamificationErrors = KairuFocus.Domain.Gamification.DomainErrors;

namespace KairuFocus.Api.Gamification;

[ApiController]
[Route("api/gamification")]
[Authorize]
public sealed class GamificationController : ControllerBase
{
    private const string InvalidPeriodError = "Invalid period.";

    private readonly IMediator _mediator;

    public GamificationController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("xp")]
    public async Task<IActionResult> GetXp(CancellationToken ct)
    {
        var result = await _mediator.SendAsync<GetXpSummaryQuery, GetXpSummaryResult>(new GetXpSummaryQuery(), ct);
        return Ok(result);
    }

    // ── Community (UC-XP-07 to UC-XP-10) ───────────────────────────────────
    // The HTTP period is translated here: handlers only ever receive a valid CommunityPeriod.

    [HttpGet("community/progress")]
    public async Task<IActionResult> GetCommunityProgress([FromQuery] string? period, CancellationToken ct)
    {
        if (!CommunityPeriods.TryParse(period, out var communityPeriod))
            return BadRequest(new { error = InvalidPeriodError });

        var result = await _mediator.SendAsync<GetCommunityProgressQuery, GetCommunityProgressResult>(
            new GetCommunityProgressQuery(communityPeriod), ct);
        return Ok(result);
    }

    [HttpGet("community/leaderboard")]
    public async Task<IActionResult> GetCommunityLeaderboard([FromQuery] string? period, CancellationToken ct)
    {
        if (!CommunityPeriods.TryParse(period, out var communityPeriod))
            return BadRequest(new { error = InvalidPeriodError });

        var result = await _mediator.SendAsync<GetCommunityLeaderboardQuery, GetCommunityLeaderboardResult>(
            new GetCommunityLeaderboardQuery(communityPeriod), ct);
        return Ok(result);
    }

    [HttpGet("community/recap")]
    public async Task<IActionResult> GetCommunityRecap([FromQuery] string? period, CancellationToken ct)
    {
        if (!CommunityPeriods.TryParse(period, out var communityPeriod))
            return BadRequest(new { error = InvalidPeriodError });

        var result = await _mediator.SendAsync<GetCommunityRecapQuery, GetCommunityRecapResult>(
            new GetCommunityRecapQuery(communityPeriod), ct);
        return Ok(result);
    }

    [HttpGet("community/presence")]
    public async Task<IActionResult> GetCommunityPresence(CancellationToken ct)
    {
        var result = await _mediator.SendAsync<GetCommunityPresenceQuery, GetCommunityPresenceResult>(
            new GetCommunityPresenceQuery(), ct);
        return Ok(result);
    }

    // ── Leaderboard membership (UC-XP-03 to UC-XP-06) ──────────────────────

    [HttpGet("leaderboard/membership")]
    public async Task<IActionResult> GetLeaderboardMembership(CancellationToken ct)
    {
        var result = await _mediator.SendAsync<GetLeaderboardMembershipQuery, GetLeaderboardMembershipResult>(
            new GetLeaderboardMembershipQuery(), ct);
        return Ok(result);
    }

    [HttpPost("leaderboard/membership")]
    public async Task<IActionResult> JoinLeaderboard(CancellationToken ct)
    {
        var result = await _mediator.DispatchAsync<JoinLeaderboardCommand, JoinLeaderboardResult>(
            new JoinLeaderboardCommand(), ct);

        return result switch
        {
            { IsSuccess: true } => CreatedAtAction(nameof(GetLeaderboardMembership), null, new { pseudonym = result.Pseudonym }),
            _ when result.Error == GamificationErrors.Gamification.AlreadyParticipating => Conflict(new { error = result.Error }),
            _ when result.Error == GamificationErrors.Gamification.PseudonymPoolExhausted => StatusCode(503, new { error = result.Error }),
            _ => StatusCode(500, new { error = result.Error })
        };
    }

    [HttpPost("leaderboard/membership/reroll")]
    public async Task<IActionResult> RerollPseudonym(CancellationToken ct)
    {
        var result = await _mediator.DispatchAsync<RerollPseudonymCommand, RerollPseudonymResult>(
            new RerollPseudonymCommand(), ct);

        return result switch
        {
            { IsSuccess: true } => Ok(new { pseudonym = result.Pseudonym }),
            _ when result.Error == GamificationErrors.Gamification.NotParticipating => NotFound(new { error = result.Error }),
            _ when result.Error == GamificationErrors.Gamification.PseudonymPoolExhausted => StatusCode(503, new { error = result.Error }),
            _ => StatusCode(500, new { error = result.Error })
        };
    }

    [HttpDelete("leaderboard/membership")]
    public async Task<IActionResult> LeaveLeaderboard(CancellationToken ct)
    {
        var result = await _mediator.DispatchAsync<LeaveLeaderboardCommand, LeaveLeaderboardResult>(
            new LeaveLeaderboardCommand(), ct);

        return result switch
        {
            { IsSuccess: true } => NoContent(),
            _ when result.Error == GamificationErrors.Gamification.NotParticipating => NotFound(new { error = result.Error }),
            _ => StatusCode(500, new { error = result.Error })
        };
    }
}
