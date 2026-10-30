using KairuFocus.Domain.Gamification;
using Microsoft.EntityFrameworkCore;

namespace KairuFocus.Infrastructure.Persistence.Repositories;

/// <summary>
/// CROSS-USERS read (ADR-024). Joins the XP ledger to the community members only (inner join) and projects
/// (Name, AvatarUrl, Xp): no user identifier leaves the query, and the Users table is never read.
/// </summary>
internal sealed class EfCoreLeaderboardStandingsRepository : ILeaderboardStandingsRepository
{
    private readonly KairuFocusDbContext _context;

    public EfCoreLeaderboardStandingsRepository(KairuFocusDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<XpStanding>> GetStandingsBetweenAsync(
        DateTime startUtc,
        DateTime endUtc,
        CancellationToken cancellationToken = default) =>
        // UTC range comparison [startUtc, endUtc) — provider-safe (ADR-020/021).
        await StandingsOf(
                _context.XpGains.Where(g => g.EarnedAtUtc >= startUtc && g.EarnedAtUtc < endUtc))
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<XpStanding>> GetAllTimeStandingsAsync(CancellationToken cancellationToken = default) =>
        await StandingsOf(_context.XpGains)
            .ToListAsync(cancellationToken);

    // XP gained before joining counts too: the opt-in governs who appears, not what is counted.
    private IQueryable<XpStanding> StandingsOf(IQueryable<XpGain> gains) =>
        from g in gains.AsNoTracking()
        join p in _context.MemberProfiles.AsNoTracking() on g.OwnerId equals p.Id
        group g.Amount by new { p.Name, p.AvatarUrl } into grp
        select new XpStanding(grp.Key.Name, grp.Key.AvatarUrl, grp.Sum());
}
