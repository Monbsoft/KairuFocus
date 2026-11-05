using KairuFocus.Application.Gamification.Common;
using KairuFocus.Application.Gamification.Queries.GetCommunityLeaderboard;
using KairuFocus.Application.Gamification.Queries.GetCommunityPresence;
using KairuFocus.Application.Gamification.Queries.GetCommunityProgress;
using KairuFocus.Application.Gamification.Queries.GetCommunityRecap;
using KairuFocus.Application.Gamification.Queries.GetXpSummary;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Monbsoft.BrilliantMediator.Abstractions;

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
}
